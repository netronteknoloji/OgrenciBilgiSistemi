using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Tests.Altyapi;

namespace OgrenciBilgiSistemi.Tests.Services
{
    /// <summary>
    /// AidatService karakterizasyon testleri — refactor güvenlik ağı.
    /// Mevcut davranışı kilitler; davranış BİLEREK değiştirilirse test de güncellenir.
    /// </summary>
    public class AidatServiceTests : VeritabaniTestTabani
    {
        private const int Yil = 2025;

        private static AidatService ServisOlustur(AppDbContext ctx) =>
            new(ctx, NullLogger<AidatService>.Instance);

        private static async Task<OgrenciAidatModel> AidatEkleAsync(
            AppDbContext ctx, int ogrenciId, decimal borc, decimal odenen = 0m, bool muaf = false)
        {
            var aidat = new OgrenciAidatModel
            {
                OgrenciId = ogrenciId,
                BaslangicYil = Yil,
                Borc = borc,
                Odenen = odenen,
                Muaf = muaf
            };
            ctx.OgrenciAidatlar.Add(aidat);
            await ctx.SaveChangesAsync();
            return aidat;
        }

        // ---------------- GetOgrenciAidatAsync ----------------

        [Fact]
        public async Task GetOgrenciAidat_KayitYoksa_BorcuTarifedenBaslatir()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);
            await TarifeEkleAsync(ctx, Yil, 1000m);

            var sonuc = await ServisOlustur(ctx).GetOgrenciAidatAsync(ogrenci.OgrenciId, Yil);

            Assert.Equal(ogrenci.OgrenciId, sonuc.OgrenciId);
            Assert.Equal("ALİ VELİ", sonuc.OgrenciAdSoyad);
            Assert.Equal(1000m, sonuc.ToplamBorc);
            Assert.Equal(0m, sonuc.ToplamOdenen);
            Assert.False(sonuc.Muaf);
            Assert.Null(sonuc.SonOdemeTarihi);
            Assert.Empty(sonuc.Odemeler);
            Assert.NotNull(sonuc.Tarife);
            Assert.Equal(1000m, sonuc.Tarife!.Tutar);
        }

        [Fact]
        public async Task GetOgrenciAidat_KayitVarsa_KayittakiDegerleriDoner()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "AYŞE YILMAZ", 102);
            var aidat = await AidatEkleAsync(ctx, ogrenci.OgrenciId, borc: 1200m, odenen: 300m);
            ctx.OgrenciAidatOdemeler.Add(new OgrenciAidatOdemeModel
            {
                OgrenciAidatId = aidat.OgrenciAidatId,
                OdemeTarihi = new DateTime(2025, 10, 5),
                Tutar = 300m,
                Aciklama = "Ekim taksiti"
            });
            await ctx.SaveChangesAsync();

            var sonuc = await ServisOlustur(ctx).GetOgrenciAidatAsync(ogrenci.OgrenciId, Yil);

            Assert.Equal(1200m, sonuc.ToplamBorc);
            Assert.Equal(300m, sonuc.ToplamOdenen);
            var satir = Assert.Single(sonuc.Odemeler);
            Assert.Equal(300m, satir.Tutar);
            Assert.Equal("Ekim taksiti", satir.Aciklama);
        }

        // ---------------- OdemeEkleAsync ----------------

        [Fact]
        public async Task OdemeEkle_AidatKaydiYoksa_TarifedenOlusturupOdemeyiIsler()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);
            await TarifeEkleAsync(ctx, Yil, 1000m);

            var tarih = new DateTime(2025, 11, 1, 14, 30, 0);
            var sonuc = await ServisOlustur(ctx).OdemeEkleAsync(new AidatOdemeEkleDto
            {
                OgrenciId = ogrenci.OgrenciId,
                Yil = Yil,
                Tutar = 250m,
                Tarih = tarih,
                Aciklama = "Kasım"
            });

            Assert.True(sonuc.OgrenciAidatOdemeId > 0);

            await using var dogrulama = YeniContext();
            var aidat = await dogrulama.OgrenciAidatlar
                .Include(a => a.Odemeler)
                .SingleAsync(a => a.OgrenciId == ogrenci.OgrenciId && a.BaslangicYil == Yil);

            Assert.Equal(1000m, aidat.Borc);     // borç tarifeden başlatıldı
            Assert.Equal(250m, aidat.Odenen);
            Assert.Equal(tarih, aidat.SonOdemeTarihi);
            Assert.Single(aidat.Odemeler);
        }

        [Fact]
        public async Task OdemeEkle_BorcSifirVeMuafDegilse_BorcuTarifeyeYukseltir()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);
            await TarifeEkleAsync(ctx, Yil, 1000m);
            await AidatEkleAsync(ctx, ogrenci.OgrenciId, borc: 0m);

            await ServisOlustur(ctx).OdemeEkleAsync(new AidatOdemeEkleDto
            {
                OgrenciId = ogrenci.OgrenciId,
                Yil = Yil,
                Tutar = 100m,
                Tarih = new DateTime(2025, 12, 1)
            });

            await using var dogrulama = YeniContext();
            var aidat = await dogrulama.OgrenciAidatlar.SingleAsync(a => a.OgrenciId == ogrenci.OgrenciId);
            Assert.Equal(1000m, aidat.Borc);
            Assert.Equal(100m, aidat.Odenen);
        }

        // ---------------- OdemeSilAsync ----------------

        [Fact]
        public async Task OdemeSil_SoftDeleteYaparOdeneniDusurerVeSonOdemeTarihiniGunceller()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);
            var aidat = await AidatEkleAsync(ctx, ogrenci.OgrenciId, borc: 1000m, odenen: 500m);

            var eskiTarih = new DateTime(2025, 10, 1);
            var yeniTarih = new DateTime(2025, 11, 1);
            var ilkOdeme = new OgrenciAidatOdemeModel { OgrenciAidatId = aidat.OgrenciAidatId, OdemeTarihi = eskiTarih, Tutar = 200m };
            var sonOdeme = new OgrenciAidatOdemeModel { OgrenciAidatId = aidat.OgrenciAidatId, OdemeTarihi = yeniTarih, Tutar = 300m };
            ctx.OgrenciAidatOdemeler.AddRange(ilkOdeme, sonOdeme);
            aidat.SonOdemeTarihi = yeniTarih;
            await ctx.SaveChangesAsync();

            var ok = await ServisOlustur(ctx).OdemeSilAsync(sonOdeme.OgrenciAidatOdemeId);

            Assert.True(ok);

            await using var dogrulama = YeniContext();
            var guncelAidat = await dogrulama.OgrenciAidatlar.SingleAsync(a => a.OgrenciAidatId == aidat.OgrenciAidatId);
            Assert.Equal(200m, guncelAidat.Odenen);            // 500 - 300
            Assert.Equal(eskiTarih, guncelAidat.SonOdemeTarihi); // kalan aktif ödemeden hesaplanır

            // Kayıt fiziksel silinmez, AktifMi=false yapılır (global filter dışına çıkar)
            var silinen = await dogrulama.OgrenciAidatOdemeler
                .IgnoreQueryFilters()
                .SingleAsync(p => p.OgrenciAidatOdemeId == sonOdeme.OgrenciAidatOdemeId);
            Assert.False(silinen.AktifMi);
        }

        [Fact]
        public async Task OdemeSil_KayitYoksa_FalseDoner()
        {
            await using var ctx = YeniContext();
            var ok = await ServisOlustur(ctx).OdemeSilAsync(123456);
            Assert.False(ok);
        }

        // ---------------- Muafiyet ----------------

        [Fact]
        public async Task SetYillikMuafiyet_KayitYoksa_TarifeBorcuylaMuafKayitOlusturur()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);
            await TarifeEkleAsync(ctx, Yil, 1000m);

            var ok = await ServisOlustur(ctx).SetYillikMuafiyetAsync(ogrenci.OgrenciId, Yil, muaf: true);

            Assert.True(ok);
            await using var dogrulama = YeniContext();
            var aidat = await dogrulama.OgrenciAidatlar.SingleAsync(a => a.OgrenciId == ogrenci.OgrenciId);
            Assert.True(aidat.Muaf);
            Assert.Equal(1000m, aidat.Borc);
        }

        [Fact]
        public async Task GetYillikMuafiyet_KayitYoksa_FalseDoner()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);

            var muaf = await ServisOlustur(ctx).GetYillikMuafiyetAsync(ogrenci.OgrenciId, Yil);

            Assert.False(muaf);
        }

        // ---------------- GetAidatRaporAsync ----------------

        private async Task<(int aId, int bId, int cId, int dId)> RaporVerisiHazirlaAsync(AppDbContext ctx)
        {
            var birim = await BirimEkleAsync(ctx);
            await TarifeEkleAsync(ctx, Yil, 1000m);

            // A: aidat kaydı yok → borç tarifeden gelir (borçlu)
            var a = await OgrenciEkleAsync(ctx, "AAA BORCLU", 1, birim.BirimId);

            // B: borcun tamamı ödenmiş (borçsuz)
            var b = await OgrenciEkleAsync(ctx, "BBB BORCSUZ", 2, birim.BirimId);
            var bAidat = await AidatEkleAsync(ctx, b.OgrenciId, borc: 1000m, odenen: 1000m);
            ctx.OgrenciAidatOdemeler.Add(new OgrenciAidatOdemeModel
            {
                OgrenciAidatId = bAidat.OgrenciAidatId,
                OdemeTarihi = new DateTime(2025, 10, 15),
                Tutar = 1000m
            });

            // C: muaf
            var c = await OgrenciEkleAsync(ctx, "CCC MUAF", 3, birim.BirimId);
            await AidatEkleAsync(ctx, c.OgrenciId, borc: 1000m, muaf: true);

            // D: pasif öğrenci
            var d = await OgrenciEkleAsync(ctx, "DDD PASIF", 4, birim.BirimId, aktif: false);

            await ctx.SaveChangesAsync();
            return (a.OgrenciId, b.OgrenciId, c.OgrenciId, d.OgrenciId);
        }

        [Fact]
        public async Task AidatRapor_TarifeFallbackMuafVeToplamlarDogru()
        {
            await using var ctx = YeniContext();
            var (aId, bId, cId, dId) = await RaporVerisiHazirlaAsync(ctx);

            var rapor = await ServisOlustur(ctx).GetAidatRaporAsync(
                yil: Yil, bas: null, bit: null, query: null, birimId: null);

            // Pasif öğrenci raporda yok
            Assert.Equal(3, rapor.Satirlar.Count);
            Assert.DoesNotContain(rapor.Satirlar, r => r.OgrenciId == dId);

            var aSatir = rapor.Satirlar.Single(r => r.OgrenciId == aId);
            Assert.Equal(1000m, aSatir.BorcGosterim);  // tarife fallback
            Assert.Equal(0m, aSatir.GosterilenOdenen);
            Assert.Equal(1000m, aSatir.Kalan);
            Assert.False(aSatir.Kapandi);

            var bSatir = rapor.Satirlar.Single(r => r.OgrenciId == bId);
            Assert.Equal(0m, bSatir.Kalan);
            Assert.True(bSatir.Kapandi);

            var cSatir = rapor.Satirlar.Single(r => r.OgrenciId == cId);
            Assert.True(cSatir.Muaf);
            Assert.Equal(0m, cSatir.BorcGosterim);
            Assert.Equal(0m, cSatir.Kalan);

            // Toplamlar muaf hariç hesaplanır
            Assert.Equal(2000m, rapor.ToplamBorc);
            Assert.Equal(1000m, rapor.ToplamOdenenGosterilen);
            Assert.Equal(1000m, rapor.ToplamKalan);
        }

        [Theory]
        [InlineData(RaporDurumFiltresiDto.Borclu, "AAA BORCLU")]
        [InlineData(RaporDurumFiltresiDto.Borcsuz, "BBB BORCSUZ")]
        [InlineData(RaporDurumFiltresiDto.Muaf, "CCC MUAF")]
        public async Task AidatRapor_DurumFiltresiDogruSatirlariSecer(RaporDurumFiltresiDto durum, string beklenenAd)
        {
            await using var ctx = YeniContext();
            await RaporVerisiHazirlaAsync(ctx);

            var rapor = await ServisOlustur(ctx).GetAidatRaporAsync(
                yil: Yil, bas: null, bit: null, query: null, birimId: null, durum: durum);

            var satir = Assert.Single(rapor.Satirlar);
            Assert.Equal(beklenenAd, satir.OgrenciAdSoyad);
        }

        [Fact]
        public async Task AidatRapor_TarihAraligi_BitisGunuHaricOdemeleriToplar()
        {
            // Karakterizasyon: NormalizeDateRange bitiş tarihini exclusive kullanır
            // (p.OdemeTarihi < bit.Date) → bitiş GÜNÜNDEKİ ödemeler toplama GİRMEZ.
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "AAA TARIH", 1);
            await TarifeEkleAsync(ctx, Yil, 1000m);
            var aidat = await AidatEkleAsync(ctx, ogrenci.OgrenciId, borc: 1000m, odenen: 600m);
            ctx.OgrenciAidatOdemeler.AddRange(
                new OgrenciAidatOdemeModel { OgrenciAidatId = aidat.OgrenciAidatId, OdemeTarihi = new DateTime(2025, 10, 1), Tutar = 100m },
                new OgrenciAidatOdemeModel { OgrenciAidatId = aidat.OgrenciAidatId, OdemeTarihi = new DateTime(2025, 10, 15), Tutar = 200m },
                new OgrenciAidatOdemeModel { OgrenciAidatId = aidat.OgrenciAidatId, OdemeTarihi = new DateTime(2025, 10, 20, 10, 0, 0), Tutar = 300m });
            await ctx.SaveChangesAsync();

            var rapor = await ServisOlustur(ctx).GetAidatRaporAsync(
                yil: Yil,
                bas: new DateTime(2025, 10, 10),
                bit: new DateTime(2025, 10, 20),
                query: null, birimId: null);

            var satir = Assert.Single(rapor.Satirlar);
            // 15 Ekim (200) dahil; 1 Ekim aralık öncesi; 20 Ekim bitiş günü → HARİÇ
            Assert.Equal(200m, satir.GosterilenOdenen);
        }

        [Fact]
        public async Task AidatRapor_IncludePasifTrue_MevcutDavranis_PasifOgrenciYineGelmez()
        {
            // Karakterizasyon: AidatService, AppDbContext.IncludePasifOgrenciler flag'ini
            // SET ETMEDİĞİ için includePasif=true parametresi fiilen etkisizdir — global
            // query filter pasif öğrencileri yine eler. (YemekhaneService flag'i set eder;
            // muhtemel bug, ayrı bir değişiklikte ele alınmalı.)
            await using var ctx = YeniContext();
            var (_, _, _, dId) = await RaporVerisiHazirlaAsync(ctx);

            var rapor = await ServisOlustur(ctx).GetAidatRaporAsync(
                yil: Yil, bas: null, bit: null, query: null, birimId: null,
                includePasif: true);

            Assert.DoesNotContain(rapor.Satirlar, r => r.OgrenciId == dId);
        }

        // ---------------- TarifeKaydetAsync ----------------

        [Fact]
        public async Task TarifeKaydet_YeniKayitVeGuncellemeUpsertCalisir()
        {
            await using var ctx = YeniContext();
            var servis = ServisOlustur(ctx);

            await servis.TarifeKaydetAsync(new TarifeDto { Yil = Yil, Tutar = 1000m, Aciklama = "İlk" });
            await servis.TarifeKaydetAsync(new TarifeDto { Yil = Yil, Tutar = 1500m, Aciklama = "Zam" });

            await using var dogrulama = YeniContext();
            var tarife = await dogrulama.OgrenciAidatTarifeler.SingleAsync(t => t.BaslangicYil == Yil);
            Assert.Equal(1500m, tarife.Tutar);
            Assert.Equal("Zam", tarife.Aciklama);
        }
    }
}
