using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class VeliProfilModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int KullaniciId { get; set; }

        [StringLength(150)]
        [Display(Name = "Veli Adres")]
        public string? VeliAdres { get; set; }

        [StringLength(50)]
        [Display(Name = "Veli Meslek")]
        public string? VeliMeslek { get; set; }

        [StringLength(100)]
        [Display(Name = "Veli İş Yeri")]
        public string? VeliIsYeri { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(100)]
        [Display(Name = "E-posta")]
        public string? VeliEmail { get; set; }

        [Display(Name = "Yakınlık")]
        public YakinlikTipi? VeliYakinlik { get; set; }

        [Display(Name = "Durum (Aktif)")]
        public bool VeliDurum { get; set; } = true;

        [NotMapped]
        public int OgrenciSayisi { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;
    }
}
