namespace OgrenciBilgiSistemi.Mobil.Models
{
    /// <summary>
    /// API'nin /ogrenciler/{id}/details endpoint'inden dönen öğrenci detay bilgileri.
    /// </summary>
    public class OgrenciDetay
    {
        public string OgrenciAdSoyad { get; set; } = "Bilinmiyor";
        public string OgrenciNo { get; set; } = "-";
        public string OgrenciKartNo { get; set; } = "-";
        public string OgrenciGorsel { get; set; } = "user_icon.png";
        public string BirimAd { get; set; } = "Atanmamış";
        public string VeliAdSoyad { get; set; } = "Belirtilmemiş";
        public string VeliTelefon { get; set; } = "-";
        public string VeliEmail { get; set; } = "-";
        public string VeliMeslek { get; set; } = "-";
        public string VeliIsYeri { get; set; } = "-";
        public string VeliAdres { get; set; } = "-";
        public string OgretmenAdSoyad { get; set; } = "Atanmamış";
        public string Plaka { get; set; } = "Kullanmıyor";
    }
}
