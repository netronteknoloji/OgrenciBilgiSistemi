namespace OgrenciBilgiSistemi.Sms;

/// <summary>
/// Toplu SMS gönderim için SemaphoreSlim'le sınırlı paralel gönderici.
/// EF/ADO.NET bilmez; sadece SMS gönderimini paralelleştirir.
/// Caller dönen sonuç listesini tek thread'de işleyip log/DB güncelleme yapmalıdır
/// (DbContext / paylaşılan SqlConnection thread-safe olmadığı için).
/// </summary>
public static class SmsParalelGonderici
{
    /// <summary>
    /// Verilen ögelerin her biri için SMS gönderir; aynı anda en fazla
    /// <paramref name="maxParalel"/> gönderim çalışır. Tüm gönderimler bitene kadar bekler
    /// ve her ögeyi kendi sonucu ile birlikte döndürür. Gönderim sırasında oluşan istisnalar
    /// yakalanıp <see cref="SmsGonderimSonucu"/>'na (Gecici kategori) normalize edilir.
    /// </summary>
    public static async Task<List<(T Oge, SmsGonderimSonucu Sonuc)>> GonderHerBiri<T>(
        ISmsService smsService,
        IReadOnlyList<T> ogeler,
        Func<T, (string Telefon, string Mesaj)> mesajSec,
        int maxParalel,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(smsService);
        ArgumentNullException.ThrowIfNull(ogeler);
        ArgumentNullException.ThrowIfNull(mesajSec);

        if (ogeler.Count == 0)
            return new List<(T, SmsGonderimSonucu)>(0);

        var paralel = maxParalel < 1 ? 1 : maxParalel;
        using var sema = new SemaphoreSlim(paralel, paralel);

        var tasks = new Task<(T, SmsGonderimSonucu)>[ogeler.Count];
        for (int i = 0; i < ogeler.Count; i++)
        {
            var oge = ogeler[i];
            tasks[i] = GonderTek(smsService, oge, mesajSec, sema, ct);
        }

        var sonuclar = await Task.WhenAll(tasks);
        return new List<(T, SmsGonderimSonucu)>(sonuclar);
    }

    private static async Task<(T, SmsGonderimSonucu)> GonderTek<T>(
        ISmsService smsService,
        T oge,
        Func<T, (string Telefon, string Mesaj)> mesajSec,
        SemaphoreSlim sema,
        CancellationToken ct)
    {
        try
        {
            await sema.WaitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return (oge, new SmsGonderimSonucu(false,
                Hata: "İşlem iptal edildi.",
                HataKategorisi: SmsHataKategorisi.Gecici));
        }

        try
        {
            var (telefon, mesaj) = mesajSec(oge);
            var sonuc = await smsService.Gonder(telefon, mesaj, ct);
            return (oge, sonuc);
        }
        catch (OperationCanceledException)
        {
            return (oge, new SmsGonderimSonucu(false,
                Hata: "İşlem iptal edildi.",
                HataKategorisi: SmsHataKategorisi.Gecici));
        }
        catch (Exception ex)
        {
            return (oge, new SmsGonderimSonucu(false,
                Hata: ex.Message,
                HamCevap: ex.ToString(),
                HataKategorisi: SmsHataKategorisi.Gecici));
        }
        finally
        {
            sema.Release();
        }
    }
}
