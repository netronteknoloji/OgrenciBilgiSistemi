namespace OgrenciBilgiSistemi.Api.Models
{
    public class VeliListeModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? Telefon { get; set; }
    }
}
