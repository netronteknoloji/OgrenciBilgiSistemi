namespace OgrenciBilgiSistemi.Api.Models
{
    public class ServisProfilModel
    {
        public int KullaniciId { get; set; }
        public string Plaka { get; set; } = string.Empty;
        public string? ServisTelefon { get; set; }
        public bool ServisDurum { get; set; }
    }
}
