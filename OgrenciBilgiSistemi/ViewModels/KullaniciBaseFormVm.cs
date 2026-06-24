using OgrenciBilgiSistemi.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KullaniciBaseFormVm
    {
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Sifre { get; set; }

        public KullaniciRolu Rol { get; set; } = KullaniciRolu.Ogretmen;
        public bool KullaniciDurum { get; set; } = true;

        [StringLength(15)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Telefon numarası yalnızca rakamlardan oluşmalıdır!")]
        public string? Telefon { get; set; }

        public int? ReturnPage { get; set; }
        public string? ReturnFilter { get; set; }

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";
        public bool IncludeId { get; set; }
    }
}
