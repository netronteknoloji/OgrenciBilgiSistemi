namespace OgrenciBilgiSistemi.Mobil.Models
{
    public sealed class ServisListeOgesi
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string? Plaka { get; set; }
        public string? ServisTelefon { get; set; }
        public int OgrenciSayisi { get; set; }
        // Eski API sürümleri göndermez → null kalabilir.
        public string? SoforAdSoyad { get; set; }
        public string? HostesAdSoyad { get; set; }
        public string? HostesTelefon { get; set; }

        public string PlakaGosterim => string.IsNullOrWhiteSpace(Plaka) ? "—" : Plaka!;
        public string TelefonGosterim => string.IsNullOrWhiteSpace(ServisTelefon) ? "—" : ServisTelefon!;
        public string SoforGosterim => string.IsNullOrWhiteSpace(SoforAdSoyad) ? KullaniciAdi : SoforAdSoyad!;
        public string HostesGosterim => string.IsNullOrWhiteSpace(HostesAdSoyad) ? "—" : HostesAdSoyad!;
        public string HostesTelefonGosterim => string.IsNullOrWhiteSpace(HostesTelefon) ? "—" : HostesTelefon!;
    }
}
