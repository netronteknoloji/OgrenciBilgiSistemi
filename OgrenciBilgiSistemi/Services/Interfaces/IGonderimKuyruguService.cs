using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    /// <summary>SMS ve push gönderimlerini GonderimKuyrugu'na alır; gönderimi GonderimKuyruguIsleyici yapar.</summary>
    public interface IGonderimKuyruguService
    {
        /// <summary>
        /// Bugünkü ana kapı (giriş/çıkış) ve yemekhane geçişlerinden SMS'i kuyruğa alınmamış olanları kuyruğa alır.
        /// <paramref name="ogrenciId"/> verilirse yalnızca o öğrenci. Tekrar çağrılması güvenlidir (aynı geçiş bir kez alınır).
        /// </summary>
        Task<int> GecisSmsleriniKuyrugaAlAsync(int? ogrenciId = null, CancellationToken ct = default);

        /// <summary>Bildirimin push'unu kuyruğa ekler; kayıt çağıranın SaveChanges'i ile birlikte yazılır.</summary>
        void PushEkle(BildirimModel bildirim);
    }
}
