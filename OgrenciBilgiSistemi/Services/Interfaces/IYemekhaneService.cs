using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IYemekhaneService
    {
        // -------- Özet / Rapor / Excel --------
        Task<YemekhaneOzetVm> GetOzetAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);

        // Yemekhane Index sayfası — seçilen ayda öğrencilerin yemek hakkı (takvim yılı/ay)
        Task<YemekhaneAylikTakipSonucDto> GetAylikTakipAsync(
            int yil,
            int ay,
            string? query,
            int? birimId,
            YemekHakkiFiltresi durum = YemekHakkiFiltresi.Hepsi,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default);

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

        /// <summary>Öğrencilerin belirtilen yıl/ay hakkını tek transaction'da açar/kapatır (not korunur). Etkilenen öğrenci sayısını döner.</summary>
        Task<int> SetAyTopluAsync(IReadOnlyCollection<int> ogrenciIdler, int yil, int ay, bool aktif, CancellationToken ct = default);

        /// <summary>Excel (A sütunu öğrenci no) o ayın tam listesidir: listedekilerin hakkı açılır, olmayanlarınki kapatılır.</summary>
        Task<YemekHakkiIceAktarmaSonucDto> ExceldenAyHakkiYukleAsync(Stream dosya, int yil, int ay, CancellationToken ct = default);

        /// <summary>"Öğrenci No" başlıklı boş yükleme şablonu (.xlsx).</summary>
        byte[] ExcelSablonu();

        // -------- "Bu Ay" yardımcıları (Eyl başlangıçlı akademik yıla göre yıl hesaplar) --------
        /// <summary>İçinde bulunulan ay için upsert. Sadece bu aya yazar.</summary>
        Task<OgrenciYemekModel> SetBuAyAsync(int ogrenciId, bool aktif, string? not = null, CancellationToken ct = default);

        /// <summary>Index sayfası için: verilen öğrenci id’leri için bu ayın durumlarını tek seferde getirir.</summary>
        Task<Dictionary<int, bool>> GetBuAyDurumlariAsync(IEnumerable<int> ogrenciIdleri, CancellationToken ct = default);

        // -------- Ödemeler --------
        Task<List<OgrenciYemekOdemeModel>> GetAkademikYilOdemeleriAsync(int ogrenciId, int akademikYil, CancellationToken ct = default);
        Task OdemeEkleAsync(int ogrenciId, int yil, int ay, decimal tutar, DateTime? tarih, string? aciklama, CancellationToken ct = default);
        Task OdemeSilAsync(int odemeId, CancellationToken ct = default);
    }
}
