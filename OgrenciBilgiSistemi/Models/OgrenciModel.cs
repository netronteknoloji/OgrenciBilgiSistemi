using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class OgrenciModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OgrenciId { get; set; }

        [Required(ErrorMessage = "Öğrenci adı soyadı zorunludur!")]
        [StringLength(50, ErrorMessage = "En fazla 50 karakter yazabilirsiniz!")]
        [Display(Name = "Ad Soyad")]
        public string OgrenciAdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Öğrenci numarası zorunludur!")]
        [Range(1, int.MaxValue, ErrorMessage = "Öğrenci numarası 0'dan büyük olmalıdır!")]
        [Display(Name = "Öğrenci No")]
        public int OgrenciNo { get; set; }

        [StringLength(10, ErrorMessage = "En fazla 10 karakter yazabilirsiniz!")]
        [Display(Name = "Kart No")]
        public string? OgrenciKartNo { get; set; }

        [Display(Name = "Veli")]
        public int? OgrenciVeliId { get; set; }

        [ForeignKey(nameof(OgrenciVeliId))]
        [ValidateNever]
        [Display(Name = "Veli")]
        public virtual OgrenciVeliModel? OgrenciVeli { get; set; }

        [Display(Name = "Öğle Çıkış")]
        public OglenCikisDurumu OgrenciCikisDurumu { get; set; } = OglenCikisDurumu.Hayir;

        [Display(Name = "Durum (Aktif)")]
        public bool OgrenciDurum { get; set; } = true;

        [Display(Name = "Öğretmen")]
        public int? PersonelId { get; set; }

        [ForeignKey(nameof(PersonelId))]
        [ValidateNever]
        [Display(Name = "Öğretmen")]
        public virtual PersonelModel? Personel { get; set; }

        [Display(Name = "Birimi")]
        public int? BirimId { get; set; }

        [ForeignKey(nameof(BirimId))]
        [ValidateNever]
        [Display(Name = "Birimi")]
        public virtual BirimModel? Birim { get; set; }

        [Display(Name = "Servis")]
        public int? ServisId { get; set; }

        [ForeignKey(nameof(ServisId))]
        [ValidateNever]
        [Display(Name = "Servis")]
        public virtual ServisModel? Servis { get; set; }

        [Display(Name = "Fotoğraf")]
        public string? OgrenciGorsel { get; set; }

        // Görsel dosyası yükleme alanı — tablo görünümünde yer almaz
        [NotMapped]
        [ValidateNever]
        [Display(Name = "Fotoğraf Yükle")]
        public IFormFile? OgrenciGorselFile { get; set; }

        // Navigasyon koleksiyonları (tablo listesinde gösterilmez)
        [ValidateNever]
        public virtual List<OgrenciDetayModel> OgrenciDetaylar { get; set; } = new();

        [ValidateNever]
        public virtual List<OgrenciYemekModel> OgrenciYemekler { get; set; } = new();

        [ValidateNever]
        public virtual List<OgrenciAidatModel> OgrenciAidatlar { get; set; } = new();

        [ValidateNever]
        public virtual List<SinifYoklamaModel> SinifYoklamalar { get; set; } = new();

        [NotMapped]
        [ValidateNever]
        public List<SelectListItem> Personeller { get; set; } = new();

        [NotMapped]
        [ValidateNever]
        public List<SelectListItem> Birimler { get; set; } = new();
    }
}