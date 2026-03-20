namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Ogrenci
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public int OgrenciNo { get; set; }
        public string? OgrenciGorsel { get; set; }
        public string? OgrenciKartNo { get; set; }
        public int OgrenciCikisDurumu { get; set; }
        public bool OgrenciDurum { get; set; }
        public int? BirimId { get; set; }
        public int? PersonelId { get; set; }
        public int? OgrenciVeliId { get; set; }
        public int? ServisId { get; set; }

        // Mobil'e özel alanlar — API'nin detay endpoint'inden Dictionary olarak doldurulur
        public string? SinifAdi { get; set; }
        public string? VeliAdSoyad { get; set; }
        public string? VeliTelefon { get; set; }
    }
}
