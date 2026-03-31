namespace OgrenciBilgiSistemi.Dtos
{
    public class OgrenciVeliRaporDto
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = "";
        public string? OgrenciNo { get; set; }
        public string? SinifAd { get; set; }
        public string? VeliKullaniciAdi { get; set; }
        public string? Yakinlik { get; set; }
        public string? VeliTelefon { get; set; }
        public string? VeliMeslek { get; set; }
        public string? VeliIsYeri { get; set; }
    }
}
