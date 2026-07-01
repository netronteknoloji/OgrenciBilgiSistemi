namespace OgrenciBilgiSistemi.Api.Models
{
    public sealed class ServisListeOgesiModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string? Plaka { get; set; }
        public string? ServisTelefon { get; set; }
        public bool IsDeleted { get; set; }
        public int OgrenciSayisi { get; set; }
    }
}
