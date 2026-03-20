using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class PersonelModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PersonelId { get; set; }

        [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
        [StringLength(120, ErrorMessage = "En fazla 120 karakter yazabilirsiniz!")]
        [Display(Name = "Ad Soyad")]
        public string PersonelAdSoyad { get; set; } = string.Empty;

        [Display(Name = "Fotoğraf")]
        public string? PersonelGorselPath { get; set; }

        [Display(Name = "Durum (Aktif)")]
        public bool PersonelDurum { get; set; } = true;

        [Display(Name = "Görevi")]
        public PersonelTipi PersonelTipi { get; set; } = PersonelTipi.Ogretmen;

        [StringLength(30, ErrorMessage = "En fazla 30 karakter yazabilirsiniz!")]
        [Display(Name = "Kart No")]
        public string? PersonelKartNo { get; set; }

        // Kurumsal bağ
        [Display(Name = "Birimi")]
        public int? BirimId { get; set; }

        [ForeignKey(nameof(BirimId))]
        [ValidateNever]
        [Display(Name = "Birimi")]
        public virtual BirimModel? Birim { get; set; }

        [NotMapped]
        [ValidateNever]
        [Display(Name = "Fotoğraf Yükle")]
        public IFormFile? PersonelGorselFile { get; set; }

        [NotMapped]
        public List<SelectListItem> Birimler { get; set; } = new();

        [StringLength(120)]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string? PersonelEmail { get; set; }

        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string? PersonelTelefon { get; set; }

        // Navigations
        [ValidateNever]
        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();

        [ValidateNever]
        public virtual List<ZiyaretciModel> Ziyaretciler { get; set; } = new();

        [ValidateNever]
        public virtual List<SinifYoklamaModel> SinifYoklamalar { get; set; } = new();
    }
}