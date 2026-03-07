namespace OgrenciBilgiSistemi.Api.Dtos
{
    // Öğrenci oluşturma ve güncelleme istekleri için ortak model
    public class OgrenciKaydetDto
    {
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        public int OgrenciNo { get; set; }
        public string? OgrenciKartNo { get; set; }
        public int OgrenciCikisDurumu { get; set; }  // 0=Hayır, 1=Evet
        public bool OgrenciDurum { get; set; } = true;
        public int? BirimId { get; set; }
        public int? PersonelId { get; set; }
        public int? OgrenciVeliId { get; set; }
        public string? OgrenciGorsel { get; set; }
    }
}
