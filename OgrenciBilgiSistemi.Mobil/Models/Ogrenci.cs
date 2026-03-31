using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Ogrenci
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public int OgrenciNo { get; set; }
        public string? OgrenciGorsel { get; set; }
        public string? OgrenciKartNo { get; set; }
        public OglenCikisDurumu OgrenciCikisDurumu { get; set; }
        public bool OgrenciDurum { get; set; }
        public int? BirimId { get; set; }
        public int? OgretmenId { get; set; }
        public int? VeliId { get; set; }
        public int? ServisId { get; set; }

        // Mobil'e özel — API'nin detay endpoint'inden Dictionary olarak doldurulur
        public string? SinifAdi { get; set; }
    }
}
