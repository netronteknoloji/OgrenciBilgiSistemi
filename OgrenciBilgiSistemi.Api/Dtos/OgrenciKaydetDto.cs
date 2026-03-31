using System.ComponentModel.DataAnnotations;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    // Öğrenci oluşturma ve güncelleme istekleri için ortak model
    public class OgrenciKaydetDto
    {
        [Required(ErrorMessage = "Öğrenci adı soyadı zorunludur.")]
        [StringLength(50, ErrorMessage = "Öğrenci adı soyadı en fazla 50 karakter olabilir.")]
        public string OgrenciAdSoyad { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Öğrenci numarası 0'dan büyük olmalıdır!")]
        public int OgrenciNo { get; set; }
        public string? OgrenciKartNo { get; set; }
        public OglenCikisDurumu OgrenciCikisDurumu { get; set; }
        public bool OgrenciDurum { get; set; } = true;
        public int? BirimId { get; set; }
        public int? OgretmenId { get; set; }
        public int? VeliId { get; set; }
        public int? ServisId { get; set; }
        public string? OgrenciGorsel { get; set; }
    }
}
