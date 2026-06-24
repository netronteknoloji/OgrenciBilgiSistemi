using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KitapDetayFormVm
    {
        public int KitapDetayId { get; set; }

        [Required(ErrorMessage = "Kitap alma tarihi zorunludur!")]
        public DateTime KitapAlTarih { get; set; } = DateTime.Now;

        public DateTime? KitapVerTarih { get; set; }

        [Required]
        public KitapDurumu KitapDurum { get; set; } = KitapDurumu.Alındı;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kitap seçiniz.")]
        public int KitapId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir öğrenci seçiniz.")]
        public int OgrenciId { get; set; }

        public IReadOnlyList<SelectListItem> Kitaplar { get; set; } = [];
        public IReadOnlyList<SelectListItem> Ogrenciler { get; set; } = [];

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";
        public bool IncludeId { get; set; }

        public static KitapDetayFormVm FromModel(KitapDetayModel m) => new()
        {
            KitapDetayId = m.KitapDetayId,
            KitapAlTarih = m.KitapAlTarih,
            KitapVerTarih = m.KitapVerTarih,
            KitapDurum = m.KitapDurum,
            KitapId = m.KitapId,
            OgrenciId = m.OgrenciId,
        };

        public KitapDetayModel ToModel() => new()
        {
            KitapDetayId = KitapDetayId,
            KitapAlTarih = KitapAlTarih,
            KitapVerTarih = KitapVerTarih,
            KitapDurum = KitapDurum,
            KitapId = KitapId,
            OgrenciId = OgrenciId,
        };
    }
}
