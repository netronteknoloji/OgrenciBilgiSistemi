using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Services.Implementations;

public sealed class SmsGonderimService : ISmsGonderimService
{
    private const string TIP = "AnaKapi";

    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly SmsAyarlari _ayar;
    private readonly ILogger<SmsGonderimService> _logger;

    public SmsGonderimService(
        AppDbContext db,
        ISmsService smsService,
        IOptions<SmsAyarlari> ayar,
        ILogger<SmsGonderimService> logger)
    {
        _db = db;
        _smsService = smsService;
        _ayar = ayar.Value;
        _logger = logger;
    }

    public async Task GecisSmsBildir(int ogrenciId, string ogrenciAdSoyad, string gecisTipi, DateTime zaman, CancellationToken ct = default)
    {
        if (!_ayar.Aktif) return;

        // Veli telefon numarasını çek
        var veliTelefon = await _db.Ogrenciler
            .AsNoTracking()
            .Where(o => o.OgrenciId == ogrenciId && o.VeliId != null)
            .Select(o => o.Veli!.Telefon)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(veliTelefon))
        {
            _logger.LogDebug("Öğrenci {OgrId} için veli telefonu bulunamadı, SMS gönderilmedi.", ogrenciId);
            return;
        }

        // Mesaj oluştur (şablon merkezi)
        var mesaj = SmsMesajSablonlari.AnaKapiGecis(ogrenciAdSoyad, zaman, gecisTipi);

        // SMS gönder
        var sonuc = await _smsService.Gonder(veliTelefon, mesaj, ct);

        // Her durumda gönderim geçmişine yaz
        _db.SmsGonderimGecmisleri.Add(new SmsGonderimGecmisiModel
        {
            OgrenciId = ogrenciId,
            Telefon = veliTelefon,
            Mesaj = mesaj,
            Tip = TIP,
            GonderimZamani = DateTime.Now,
            Basarili = sonuc.Basarili,
            HataKategorisi = (int)sonuc.HataKategorisi,
            Hata = sonuc.Hata,
            HamCevap = sonuc.HamCevap,
            HttpDurumKodu = sonuc.HttpDurumKodu,
            DenemeNumarasi = 1
        });

        // Başarı veya kalıcı hata: bayrağı set et (retry'a düşmesin)
        if (sonuc.Basarili || sonuc.HataKategorisi == SmsHataKategorisi.Kalici)
        {
            var bugun = zaman.Date;
            var yarin = bugun.AddDays(1);

            var detay = await _db.OgrenciDetaylar
                .Where(x => x.OgrenciId == ogrenciId
                    && x.IstasyonTipi == IstasyonTipi.AnaKapi
                    && ((x.OgrenciGTarih >= bugun && x.OgrenciGTarih < yarin)
                        || (x.OgrenciCTarih >= bugun && x.OgrenciCTarih < yarin)))
                .OrderByDescending(x => x.OgrenciGTarih ?? x.OgrenciCTarih)
                .FirstOrDefaultAsync(ct);

            if (detay is not null)
                detay.OgrenciSmsGonderildi = true;
        }

        await _db.SaveChangesAsync(ct);

        if (sonuc.Basarili)
        {
            _logger.LogInformation("SMS gönderildi. Öğrenci: {OgrId}, Telefon: {Tel}, Tip: {Tip}",
                ogrenciId, veliTelefon, gecisTipi);
        }
        else if (sonuc.HataKategorisi == SmsHataKategorisi.Kalici)
        {
            _logger.LogWarning("SMS kalıcı hata - retry yok. Öğrenci: {OgrId}, Hata: {Hata}",
                ogrenciId, sonuc.Hata);
        }
        else
        {
            _logger.LogWarning("SMS gönderilemedi (geçici). Öğrenci: {OgrId}, Hata: {Hata}",
                ogrenciId, sonuc.Hata);
        }
    }
}
