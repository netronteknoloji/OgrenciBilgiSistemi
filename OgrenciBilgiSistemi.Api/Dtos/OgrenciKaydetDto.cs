using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    // Öğrenci oluşturma ve güncelleme istekleri için ortak model
    public class OgrenciKaydetDto
    {
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Öğrenci numarası 0'dan büyük olmalıdır!")]
        public int OgrenciNo { get; set; }
        public string? OgrenciKartNo { get; set; }
        public int OgrenciCikisDurumu { get; set; }  // 0=Hayır, 1=Evet
        public bool OgrenciDurum { get; set; } = true;
        public int? BirimId { get; set; }
        public int? OgretmenId { get; set; }
        public int? VeliId { get; set; }
        public string? OgrenciGorsel { get; set; }
    }
}
