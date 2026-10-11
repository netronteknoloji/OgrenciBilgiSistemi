namespace OgrenciBilgiSistemi.Api.Models
{
    public sealed class ServisListeOgesiModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string? Plaka { get; set; }
        public string? ServisTelefon { get; set; }
        public string? SoforAdSoyad { get; set; }
        public string? HostesAdSoyad { get; set; }
        public string? HostesTelefon { get; set; }
        public bool IsDeleted { get; set; }
        public int OgrenciSayisi { get; set; }
    }
}
