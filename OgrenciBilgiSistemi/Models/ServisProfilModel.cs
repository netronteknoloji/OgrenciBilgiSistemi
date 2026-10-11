using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class ServisProfilModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Plaka zorunludur!")]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        // Şoför bilgileri (telefon Kullanici.Telefon'da tutulur)
        [StringLength(100)]
        [Display(Name = "Şoför Adı Soyadı")]
        public string? SoforAdSoyad { get; set; }

        [StringLength(11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "T.C. Kimlik No 11 haneli rakamlardan oluşmalıdır!")]
        [Display(Name = "Şoför T.C. Kimlik No")]
        public string? SoforTcKimlikNo { get; set; }

        // Hostes bilgileri
        [StringLength(100)]
        [Display(Name = "Hostes Adı Soyadı")]
        public string? HostesAdSoyad { get; set; }

        [StringLength(11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "T.C. Kimlik No 11 haneli rakamlardan oluşmalıdır!")]
        [Display(Name = "Hostes T.C. Kimlik No")]
        public string? HostesTcKimlikNo { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Telefon numarası yalnızca rakamlardan oluşmalıdır!")]
        [Display(Name = "Hostes Telefon")]
        public string? HostesTelefon { get; set; }

        // Evrak takibi: tam değilse eksik evraklar not olarak tutulur
        [Display(Name = "Evraklar tam ve eksiksiz teslim edildi")]
        public bool EvrakTamMi { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Eksik Evraklar")]
        public string? EksikEvraklar { get; set; }

        // true = silinmiş (soft delete). Eski ServisDurum (aktif/pasif) IsDeleted'a birleştirildi.
        [Display(Name = "Durum")]
        public bool IsDeleted { get; set; } = false;

        [NotMapped]
        public int OgrenciSayisi { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;
    }
}
