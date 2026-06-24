using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class BirimFormVm
    {
        public int BirimId { get; set; }

        [Required(ErrorMessage = "Birim adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Birim adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Birim Adı")]
        public string BirimAd { get; set; } = string.Empty;

        [Display(Name = "Aktif Mi?")]
        public bool BirimDurum { get; set; } = true;

        [Display(Name = "Sınıf Mı?")]
        public bool BirimSinifMi { get; set; } = true;

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";
        public bool IncludeId { get; set; }

        public static BirimFormVm FromModel(BirimModel m) => new()
        {
            BirimId = m.BirimId,
            BirimAd = m.BirimAd,
            BirimDurum = m.BirimDurum,
            BirimSinifMi = m.BirimSinifMi,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
            IncludeId = true,
        };

        public BirimModel ToModel() => new()
        {
            BirimId = BirimId,
            BirimAd = BirimAd,
            BirimDurum = BirimDurum,
            BirimSinifMi = BirimSinifMi,
        };
    }
}
