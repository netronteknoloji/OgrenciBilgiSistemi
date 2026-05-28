using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OgrenciBilgiSistemi.Push;

public sealed class FirebasePushBildirimGonderici : IPushBildirimGonderici
{
    private readonly IBildirimTokenDeposu _tokenDeposu;
    private readonly PushAyarlari _ayarlar;
    private readonly ILogger<FirebasePushBildirimGonderici> _logger;

    public FirebasePushBildirimGonderici(
        IBildirimTokenDeposu tokenDeposu,
        IOptions<PushAyarlari> ayarlar,
        ILogger<FirebasePushBildirimGonderici> logger)
    {
        _tokenDeposu = tokenDeposu;
        _ayarlar = ayarlar.Value;
        _logger = logger;
    }

    public async Task<PushGonderimSonucu> GonderAsync(int aliciKullaniciId, PushBildirimYuku yuk, CancellationToken ct = default)
    {
        if (!_ayarlar.Aktif)
            return new PushGonderimSonucu(0, 0, Array.Empty<string>());

        var tokenlar = await _tokenDeposu.AktifTokenleriGetirAsync(aliciKullaniciId, ct);
        if (tokenlar.Count == 0)
            return new PushGonderimSonucu(0, 0, Array.Empty<string>());

        return await GonderAsync(tokenlar, yuk, ct);
    }

    public async Task<PushGonderimSonucu> GonderAsync(IReadOnlyList<BildirimTokenKaydi> tokenlar, PushBildirimYuku yuk, CancellationToken ct = default)
    {
        if (!_ayarlar.Aktif || tokenlar.Count == 0)
            return new PushGonderimSonucu(0, 0, Array.Empty<string>());

        if (FirebaseApp.DefaultInstance is null)
        {
            _logger.LogWarning(
                "FirebaseApp başlatılmamış. Push gönderilemedi. " +
                "FIREBASE_CREDENTIALS_PATH env variable veya Push:ServiceAccountJsonYolu yapılandırmasını kontrol edin.");
            return new PushGonderimSonucu(0, tokenlar.Count, Array.Empty<string>());
        }

        var gecersizTokenlar = new List<string>();
        var basariliToplam = 0;
        var basarisizToplam = 0;

        // FCM SendEachAsync sınırı 500 token/istek
        const int yigingBoyutu = 500;
        for (var i = 0; i < tokenlar.Count; i += yigingBoyutu)
        {
            var dilim = tokenlar.Skip(i).Take(yigingBoyutu).ToList();
            var mesajlar = dilim.Select(t => MesajOlustur(t, yuk)).ToList();

            BatchResponse cevap;
            try
            {
                cevap = await FirebaseMessaging.DefaultInstance.SendEachAsync(mesajlar, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM toplu gönderim hatası. Yığın boyutu: {boyut}", dilim.Count);
                basarisizToplam += dilim.Count;
                continue;
            }

            basariliToplam += cevap.SuccessCount;
            basarisizToplam += cevap.FailureCount;

            for (var j = 0; j < cevap.Responses.Count; j++)
            {
                var sonuc = cevap.Responses[j];
                if (sonuc.IsSuccess) continue;

                var hata = sonuc.Exception;
                if (hata is null) continue;

                if (hata.MessagingErrorCode is MessagingErrorCode.Unregistered
                    or MessagingErrorCode.InvalidArgument
                    or MessagingErrorCode.SenderIdMismatch)
                {
                    gecersizTokenlar.Add(dilim[j].Token);
                }
                else
                {
                    _logger.LogWarning("FCM gönderim hatası kodu: {kod}", hata.MessagingErrorCode);
                }
            }
        }

        if (gecersizTokenlar.Count > 0)
        {
            try
            {
                await _tokenDeposu.IptalAsync(gecersizTokenlar, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geçersiz FCM token'ları iptal edilemedi. Sayı: {sayi}", gecersizTokenlar.Count);
            }
        }

        return new PushGonderimSonucu(basariliToplam, basarisizToplam, gecersizTokenlar);
    }

    private Message MesajOlustur(BildirimTokenKaydi kayit, PushBildirimYuku yuk)
    {
        var mesaj = new Message
        {
            Token = kayit.Token,
            Notification = new Notification
            {
                Title = yuk.Baslik,
                Body = yuk.Govde
            },
            Data = yuk.Veri.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        if (kayit.Platform == PushPlatformu.iOS)
        {
            mesaj.Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-push-type"] = "alert",
                    ["apns-priority"] = "10"
                },
                Aps = new Aps
                {
                    Alert = new ApsAlert
                    {
                        Title = yuk.Baslik,
                        Body = yuk.Govde
                    },
                    Sound = "default",
                    MutableContent = true
                }
            };
        }
        else
        {
            mesaj.Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = _ayarlar.AndroidVarsayilanKanal,
                    Sound = "default"
                }
            };
        }

        return mesaj;
    }
}
