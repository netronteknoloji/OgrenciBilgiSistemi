using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KitapFormVm
    {
        public int KitapId { get; set; }

        [Required(ErrorMessage = "Kitap adı zorunludur!")]
        [StringLength(50, ErrorMessage = "Kitap adı en fazla 50 karakter olabilir.")]
        public string KitapAd { get; set; } = string.Empty;

        public string? KitapGorsel { get; set; }

        [StringLength(30, ErrorMessage = "Kitap türü en fazla 30 karakter olabilir.")]
        public string? KitapTurAd { get; set; }

        [Range(1, 365, ErrorMessage = "Kitap gün sayısı 1 ile 365 arasında olmalıdır.")]
        public int KitapGun { get; set; }

        public bool IsDeleted { get; set; } = false;

        public IFormFile? KitapGorselFile { get; set; }

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";
        public bool IncludeId { get; set; }

        public static KitapFormVm FromModel(KitapModel m) => new()
        {
            KitapId = m.KitapId,
            KitapAd = m.KitapAd,
            KitapGorsel = m.KitapGorsel,
            KitapTurAd = m.KitapTurAd,
            KitapGun = m.KitapGun,
            IsDeleted = m.IsDeleted,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
            IncludeId = true,
        };

        public KitapModel ToModel() => new()
        {
            KitapId = KitapId,
            KitapAd = KitapAd,
            KitapGorsel = KitapGorsel,
            KitapTurAd = KitapTurAd,
            KitapGun = KitapGun,
            IsDeleted = IsDeleted,
        };
    }
}
