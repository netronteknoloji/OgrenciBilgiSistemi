using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class GonderimKuyruguService : IGonderimKuyruguService
    {
        private readonly AppDbContext _db;
        private readonly SmsAyarlari _smsAyar;
        private readonly TimeProvider _saat;
        private readonly ILogger<GonderimKuyruguService> _logger;

        public GonderimKuyruguService(AppDbContext db, IOptions<SmsAyarlari> smsAyar, TimeProvider saat,
            ILogger<GonderimKuyruguService> logger)
        {
            _db = db;
            _smsAyar = smsAyar.Value;
            _saat = saat;
            _logger = logger;
        }

        public async Task<int> GecisSmsleriniKuyrugaAlAsync(int? ogrenciId = null, CancellationToken ct = default)
        {
            // SMS kapalıyken kuyruğa alınmaz; geçişler işaretsiz kalır, açılınca aynı gün içinde alınır
            if (!_smsAyar.Aktif) return 0;

            var simdi = _saat.GetLocalNow().DateTime;
            var bugun = simdi.Date;
            var yarin = bugun.AddDays(1);

            var sorgu = _db.OgrenciDetaylar.AsNoTracking()
                .Where(d => d.OgrenciSmsGonderildi != true
                            && (d.IstasyonTipi == IstasyonTipi.AnaKapi || d.IstasyonTipi == IstasyonTipi.Yemekhane)
                            && ((d.OgrenciGTarih >= bugun && d.OgrenciGTarih < yarin)
                                || (d.OgrenciCTarih >= bugun && d.OgrenciCTarih < yarin))
                            && d.Ogrenci.Veli != null && d.Ogrenci.Veli.Telefon != null && d.Ogrenci.Veli.Telefon != "");
            if (ogrenciId is not null)
                sorgu = sorgu.Where(d => d.OgrenciId == ogrenciId);

            var adaylar = await sorgu
                .Select(d => new
                {
                    d.OgrenciDetayId,
                    d.OgrenciId,
                    d.IstasyonTipi,
                    d.OgrenciGTarih,
                    d.OgrenciCTarih,
                    AdSoyad = d.Ogrenci.OgrenciAdSoyad,
                    Telefon = d.Ogrenci.Veli!.Telefon!
                })
                .ToListAsync(ct);

            var eklenen = 0;
            foreach (var a in adaylar)
            {
                var girisMi = a.OgrenciGTarih is not null;
                var zaman = (a.OgrenciGTarih ?? a.OgrenciCTarih)!.Value;
                var yemekhaneMi = a.IstasyonTipi == IstasyonTipi.Yemekhane;
                if (yemekhaneMi && !girisMi) continue;

                var satir = new GonderimKuyruguModel
                {
                    Kanal = GonderimKanali.Sms,
                    Tip = yemekhaneMi ? "Yemekhane" : "AnaKapi",
                    TekilAnahtar = GonderimKuyruguKurallari.GecisAnahtari(a.OgrenciDetayId),
                    OgrenciId = a.OgrenciId,
                    Telefon = a.Telefon,
                    Mesaj = yemekhaneMi
                        ? SmsMesajSablonlari.YemekhaneGiris(a.AdSoyad, zaman)
                        : SmsMesajSablonlari.AnaKapiGecis(a.AdSoyad, zaman, girisMi ? GecisKurallari.Giris : GecisKurallari.Cikis),
                    Durum = GonderimDurumu.Bekliyor,
                    SonrakiDeneme = simdi,
                    GecerlilikBitis = GonderimKuyruguKurallari.SmsGecerlilikBitis(zaman),
                    OlusturmaZamani = simdi
                };

                try
                {
                    if (await TekilKuyrugaAlAsync(a.OgrenciDetayId, satir, ct))
                        eklenen++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Bir satırın hatası diğerlerini durdurmaz; işaretsiz kaldığı için sonraki taramada tekrar denenir
                    _logger.LogError(ex, "Geçiş SMS'i kuyruğa alınamadı. OgrenciDetayId: {Id}", a.OgrenciDetayId);
                }
                finally
                {
                    _db.ChangeTracker.Clear();
                }
            }

            return eklenen;
        }

        // Geçiş satırını işaretle + kuyruğa ekle, tek transaction'da. İşaretleme UPDATE'i satırı kilitler:
        // aynı geçişi eşzamanlı alan ikinci çağrı 0 satır günceller ve kuyruğa eklemez.
        private async Task<bool> TekilKuyrugaAlAsync(int ogrenciDetayId, GonderimKuyruguModel satir, CancellationToken ct)
        {
            var strateji = _db.Database.CreateExecutionStrategy();
            return await strateji.ExecuteAsync(async () =>
            {
                _db.ChangeTracker.Clear();
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var isaretlenen = await _db.OgrenciDetaylar
                    .Where(d => d.OgrenciDetayId == ogrenciDetayId && d.OgrenciSmsGonderildi != true)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.OgrenciSmsGonderildi, true), ct);

                var eklendi = false;
                if (isaretlenen == 1 && !await _db.GonderimKuyrugu.AnyAsync(k => k.TekilAnahtar == satir.TekilAnahtar, ct))
                {
                    _db.GonderimKuyrugu.Add(satir);
                    await _db.SaveChangesAsync(ct);
                    eklendi = true;
                }

                await tx.CommitAsync(ct);
                return eklendi;
            });
        }

        public void PushEkle(BildirimModel bildirim)
        {
            var simdi = _saat.GetLocalNow().DateTime;
            _db.GonderimKuyrugu.Add(new GonderimKuyruguModel
            {
                Kanal = GonderimKanali.Push,
                Tip = "Bildirim",
                AliciKullaniciId = bildirim.AliciKullaniciId,
                Bildirim = bildirim,
                Durum = GonderimDurumu.Bekliyor,
                SonrakiDeneme = simdi,
                GecerlilikBitis = GonderimKuyruguKurallari.PushGecerlilikBitis(simdi),
                OlusturmaZamani = simdi
            });
        }
    }
}
