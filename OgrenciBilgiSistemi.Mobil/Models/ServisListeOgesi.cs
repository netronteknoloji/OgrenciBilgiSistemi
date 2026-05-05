namespace OgrenciBilgiSistemi.Mobil.Models
{
    public sealed class ServisListeOgesi
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public string? Plaka { get; set; }
        public string? ServisTelefon { get; set; }
        public bool ServisDurum { get; set; }
        public int OgrenciSayisi { get; set; }

        public string DurumMetni => ServisDurum ? "Aktif" : "Pasif";
        public string PlakaGosterim => string.IsNullOrWhiteSpace(Plaka) ? "—" : Plaka!;
        public string TelefonGosterim => string.IsNullOrWhiteSpace(ServisTelefon) ? "—" : ServisTelefon!;
    }
}
