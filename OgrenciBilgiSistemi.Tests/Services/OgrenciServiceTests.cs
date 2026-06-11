using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Tests.Altyapi;

namespace OgrenciBilgiSistemi.Tests.Services
{
    /// <summary>
    /// OgrenciService karakterizasyon testleri — refactor güvenlik ağı.
    /// </summary>
    public class OgrenciServiceTests : VeritabaniTestTabani
    {
        private readonly SahteCihazService _cihaz = new();
        private readonly SahteYemekhaneService _yemekhane = new();
        private readonly SahteFileStorage _dosyalar = new();

        private OgrenciService ServisOlustur(AppDbContext ctx) =>
            new(ctx, _cihaz, _yemekhane, _dosyalar, NullLogger<OgrenciService>.Instance);

        private static async Task<KullaniciModel> OgretmenEkleAsync(AppDbContext ctx, int birimId)
        {
            var kullanici = new KullaniciModel
            {
                KullaniciAdi = $"ogretmen_{Guid.NewGuid():N}",
                Sifre = "hash",
                Rol = KullaniciRolu.Ogretmen
            };
            ctx.Kullanicilar.Add(kullanici);
            await ctx.SaveChangesAsync();

            ctx.OgretmenProfiller.Add(new OgretmenProfilModel
            {
                KullaniciId = kullanici.KullaniciId,
                BirimId = birimId,
                OgretmenDurum = true
            });
            await ctx.SaveChangesAsync();
            return kullanici;
        }

        // ---------------- EkleAsync ----------------

        [Fact]
        public async Task Ekle_AdiTurkceBuyukHarfeCevirirKartNoNormalizeEderOgretmenAtar()
        {
            await using var ctx = YeniContext();
            var birim = await BirimEkleAsync(ctx);
            var ogretmen = await OgretmenEkleAsync(ctx, birim.BirimId);

            var model = new OgrenciModel
            {
                OgrenciAdSoyad = "ali veli",
                OgrenciNo = 101,
                OgrenciKartNo = "0012345",
                BirimId = birim.BirimId
            };

            var id = await ServisOlustur(ctx).EkleAsync(model, gorsel: null, buAyYemekhaneAktif: true);

            Assert.True(id > 0);

            await using var dogrulama = YeniContext();
            var ent = await dogrulama.Ogrenciler.SingleAsync(o => o.OgrenciId == id);
            Assert.Equal("ALİ VELİ", ent.OgrenciAdSoyad);          // tr-TR: i → İ
            Assert.Equal("12345", ent.OgrenciKartNo);              // baştaki sıfırlar atılır
            Assert.Equal(ogretmen.KullaniciId, ent.OgretmenId);    // birimden öğretmen atanır

            // Yemekhane durumu yalnızca bu ay için yazılır
            var cagri = Assert.Single(_yemekhane.SetBuAyCagrilari);
            Assert.Equal((id, true), cagri);
        }

        // ---------------- GuncelleAsync ----------------

        [Fact]
        public async Task Guncelle_AlanlariGuncellerVeNormalizasyonUygular()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ESKI AD", 101, kartNo: "111");

            var guncel = new OgrenciModel
            {
                OgrenciId = ogrenci.OgrenciId,
                OgrenciAdSoyad = "yeni isim",
                OgrenciNo = 202,
                OgrenciKartNo = "00777",
                OgrenciDurum = true
            };

            await ServisOlustur(ctx).GuncelleAsync(guncel, gorsel: null, buAyYemekhaneAktif: null);

            await using var dogrulama = YeniContext();
            var ent = await dogrulama.Ogrenciler.SingleAsync(o => o.OgrenciId == ogrenci.OgrenciId);
            Assert.Equal("YENİ İSİM", ent.OgrenciAdSoyad);
            Assert.Equal(202, ent.OgrenciNo);
            Assert.Equal("777", ent.OgrenciKartNo);

            // buAyYemekhaneAktif=null → yemekhane ve cihaz senkronu çağrılmaz
            Assert.Empty(_yemekhane.SetBuAyCagrilari);
            Assert.Empty(_cihaz.GuncellenenOgrenciler);
            Assert.Empty(_cihaz.SilinenOgrenciIdleri);
        }

        [Fact]
        public async Task Guncelle_OgrenciYoksa_KeyNotFoundFirlatir()
        {
            await using var ctx = YeniContext();
            var model = new OgrenciModel { OgrenciId = 99999, OgrenciAdSoyad = "YOK", OgrenciNo = 1 };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                ServisOlustur(ctx).GuncelleAsync(model, gorsel: null, buAyYemekhaneAktif: null));
        }

        // ---------------- SilAsync ----------------

        [Fact]
        public async Task Sil_FizikselSilmezOgrenciDurumuPasifYapar()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "SILINECEK", 101);

            await ServisOlustur(ctx).SilAsync(ogrenci.OgrenciId);

            await using var dogrulama = YeniContext();
            // Global query filter pasifleri gizler → kayıt görünmez olur ama silinmemiştir
            Assert.Null(await dogrulama.Ogrenciler.FirstOrDefaultAsync(o => o.OgrenciId == ogrenci.OgrenciId));

            var ent = await dogrulama.Ogrenciler.IgnoreQueryFilters()
                .SingleAsync(o => o.OgrenciId == ogrenci.OgrenciId);
            Assert.False(ent.OgrenciDurum);
        }

        // ---------------- SearchPagedAsync ----------------

        private async Task AramaVerisiHazirlaAsync(AppDbContext ctx)
        {
            await OgrenciEkleAsync(ctx, "ALİ VELİ", 101, kartNo: "5551");
            await OgrenciEkleAsync(ctx, "AYŞE YILMAZ", 102, kartNo: "5552");
            await OgrenciEkleAsync(ctx, "PASİF ÖĞRENCİ", 103, aktif: false);
        }

        [Fact]
        public async Task SearchPaged_VarsayilanFiltre_SadeceAktifleriDoner()
        {
            await using var ctx = YeniContext();
            await AramaVerisiHazirlaAsync(ctx);

            var sonuc = await ServisOlustur(ctx).SearchPagedAsync(
                sortOrder: null, searchString: null, pageNumber: 1, birimId: null);

            Assert.Equal(2, sonuc.Count);
            Assert.DoesNotContain(sonuc, o => !o.OgrenciDurum);
        }

        [Fact]
        public async Task SearchPaged_PasifFiltre_ContextFlagiKapaliyken_BosDoner()
        {
            // Karakterizasyon: OgrenciFiltre.Pasif tek başına yeterli DEĞİL —
            // global query filter pasifleri gizlediği için çağıranın
            // AppDbContext.IncludePasifOgrenciler=true yapması gerekir.
            await using var ctx = YeniContext();
            await AramaVerisiHazirlaAsync(ctx);

            var sonuc = await ServisOlustur(ctx).SearchPagedAsync(
                sortOrder: null, searchString: null, pageNumber: 1, birimId: null,
                filtre: OgrenciFiltre.Pasif);

            Assert.Empty(sonuc);
        }

        [Fact]
        public async Task SearchPaged_PasifFiltre_ContextFlagiAcikken_PasifleriDoner()
        {
            await using var ctx = YeniContext();
            await AramaVerisiHazirlaAsync(ctx);

            ctx.IncludePasifOgrenciler = true;
            var sonuc = await ServisOlustur(ctx).SearchPagedAsync(
                sortOrder: null, searchString: null, pageNumber: 1, birimId: null,
                filtre: OgrenciFiltre.Pasif);

            var ogrenci = Assert.Single(sonuc);
            Assert.Equal("PASİF ÖĞRENCİ", ogrenci.OgrenciAdSoyad);
        }

        [Fact]
        public async Task SearchPaged_AdSoyadAramasi_TurkceCollationIleEslesir()
        {
            await using var ctx = YeniContext();
            await AramaVerisiHazirlaAsync(ctx);

            var sonuc = await ServisOlustur(ctx).SearchPagedAsync(
                sortOrder: null, searchString: "veli", pageNumber: 1, birimId: null);

            var ogrenci = Assert.Single(sonuc);
            Assert.Equal("ALİ VELİ", ogrenci.OgrenciAdSoyad);
        }

        [Fact]
        public async Task SearchPaged_SayisalArama_OgrenciNoVeKartNoIleEslesir()
        {
            await using var ctx = YeniContext();
            await AramaVerisiHazirlaAsync(ctx);

            var servis = ServisOlustur(ctx);

            var noSonuc = await servis.SearchPagedAsync(null, "101", 1, null);
            Assert.Contains(noSonuc, o => o.OgrenciNo == 101);

            var kartSonuc = await servis.SearchPagedAsync(null, "5552", 1, null);
            var kartOgrenci = Assert.Single(kartSonuc);
            Assert.Equal("AYŞE YILMAZ", kartOgrenci.OgrenciAdSoyad);
        }

        [Fact]
        public async Task SearchPaged_NoDescSiralama_BuyuktenKucugeSiralar()
        {
            await using var ctx = YeniContext();
            await AramaVerisiHazirlaAsync(ctx);

            var sonuc = await ServisOlustur(ctx).SearchPagedAsync(
                sortOrder: "No_desc", searchString: null, pageNumber: 1, birimId: null);

            Assert.Equal(new[] { 102, 101 }, sonuc.Select(o => o.OgrenciNo).ToArray());
        }

        // ---------------- GetByIdAsync ----------------

        [Fact]
        public async Task GetById_VarsaDonerYoksaNull()
        {
            await using var ctx = YeniContext();
            var ogrenci = await OgrenciEkleAsync(ctx, "ALİ VELİ", 101);

            var servis = ServisOlustur(ctx);

            var bulunan = await servis.GetByIdAsync(ogrenci.OgrenciId);
            Assert.NotNull(bulunan);
            Assert.Equal("ALİ VELİ", bulunan!.OgrenciAdSoyad);

            Assert.Null(await servis.GetByIdAsync(99999));
        }
    }
}
