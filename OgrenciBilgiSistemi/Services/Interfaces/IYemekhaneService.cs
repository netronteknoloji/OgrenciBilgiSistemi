using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IYemekhaneService
    {
        // -------- Özet / Rapor / Excel --------
        Task<YemekhaneOzetVm> GetOzetAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);

        Task<YemekhaneRaporVm> GetTopluRaporAsync(
            DateTime? bas,
            DateTime? bit,
            string? q,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<byte[]> ExportTopluRaporExcelAsync(
            DateTime? bas,
            DateTime? bit,
            string? q,
            CancellationToken ct = default);

        // -------- Tarife --------
        Task<OgrenciYemekTarifeModel?> GetTarifeAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);
        Task SetTarifeAsync(int ogrenciId, int akademikYil, decimal aylikTutar, string? aciklama = null, CancellationToken ct = default);

        // -------- Ay bazlı tekil kayıt (takvim yılı/ay) --------
        /// <summary>Belirtilen yıl/ay için kaydı upsert eder.</summary>
        Task<OgrenciYemekModel> SetAyAsync(int ogrenciId, int yil, int ay, bool aktif, string? not = null, CancellationToken ct = default);

        /// <summary>Belirtilen yıl/ay kaydını getirir (yoksa null).</summary>
        Task<OgrenciYemekModel?> GetAyAsync(int ogrenciId, int yil, int ay, CancellationToken ct = default);

        // -------- "Bu Ay" yardımcıları (Eyl başlangıçlı akademik yıla göre yıl hesaplar) --------
        /// <summary>İçinde bulunulan ay için upsert. Sadece bu aya yazar.</summary>
        Task<OgrenciYemekModel> SetBuAyAsync(int ogrenciId, bool aktif, string? not = null, CancellationToken ct = default);

        /// <summary>İçinde bulunulan ay için mevcut durum (true/false). Kayıt yoksa null.</summary>
        Task<bool?> GetBuAyDurumAsync(int ogrenciId, CancellationToken ct = default);

        /// <summary>İçinde bulunulan ay için durumu toggle eder (yoksa kayıt oluşturur).</summary>
        Task<OgrenciYemekModel> ToggleBuAyAsync(int ogrenciId, CancellationToken ct = default);

        /// <summary>Index sayfası için: verilen öğrenci id’leri için bu ayın durumlarını tek seferde getirir.</summary>
        Task<Dictionary<int, bool>> GetBuAyDurumlariAsync(IEnumerable<int> ogrenciIdleri, CancellationToken ct = default);

        // -------- Ödemeler --------
        Task<List<OgrenciYemekOdemeModel>> GetAkademikYilOdemeleriAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);
        Task OdemeEkleAsync(int ogrenciId, int yil, int ay, decimal tutar, DateTime? tarih, string? aciklama, CancellationToken ct = default);
        Task OdemeSilAsync(int odemeId, CancellationToken ct = default);
    }
}



//using OgrenciBilgiSistemi.Models;
//using OgrenciBilgiSistemi.ViewModels;

//namespace OgrenciBilgiSistemi.Services
//{
//    public interface IYemekhaneService
//    {
//        // Özet (akademik yıl: Eyl–Ağu)
//        Task<YemekhaneOzetVm> GetOzetAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);

//        // Toplu rapor – TARİH BAZLI (bas/bit) + arama + sayfalama
//        Task<YemekhaneRaporVm> GetTopluRaporAsync(
//            DateTime? bas,
//            DateTime? bit,
//            string? q,
//            int page,
//            int pageSize,
//            CancellationToken ct = default);

//        // Excel export – TARİH BAZLI (bas/bit) + arama
//        Task<byte[]> ExportTopluRaporExcelAsync(
//            DateTime? bas,
//            DateTime? bit,
//            string? q,
//            CancellationToken ct = default);

//        // Tarife (akademik yıl başlangıcı)
//        Task<OgrenciYemekTarifeModel?> GetTarifeAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);
//        Task SetTarifeAsync(int ogrenciId, int akademikYil, decimal aylikTutar, string? aciklama = null, CancellationToken ct = default);

//        // Ay durumu (takvim yılı/ay gönderilir)
//        Task<OgrenciYemekModel> SetAyAsync(int ogrenciId, int yil, int ay, bool aktif, string? not = null, CancellationToken ct = default);

//        // Ödemeler
//        // Akademik yılın tamamı (Eyl..Ara + Oca..Ağu)
//        Task<List<OgrenciYemekOdemeModel>> GetAkademikYilOdemeleriAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);
//        // Ödeme ekleme/silme (takvim yılı/ay)
//        Task OdemeEkleAsync(int ogrenciId, int yil, int ay, decimal tutar, DateTime? tarih, string? aciklama, CancellationToken ct = default);
//        Task OdemeSilAsync(int odemeId, CancellationToken ct = default);
//    }
//}
