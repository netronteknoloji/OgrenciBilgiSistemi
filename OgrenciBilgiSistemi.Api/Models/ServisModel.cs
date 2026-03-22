namespace OgrenciBilgiSistemi.Api.Models
{
    public class ServisModel
    {
        public int ServisId { get; set; }
        public string Plaka { get; set; } = string.Empty;
        public int? KullaniciId { get; set; }
    }
}
