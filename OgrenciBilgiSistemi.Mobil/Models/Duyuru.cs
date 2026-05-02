namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Duyuru
    {
        public int DuyuruId { get; set; }
        public int OlusturanKullaniciId { get; set; }
        public string OlusturanAdSoyad { get; set; } = string.Empty;
        public int Hedef { get; set; }
        public string Baslik { get; set; } = string.Empty;
        public string Icerik { get; set; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; set; }

        public string TarihMetni => OlusturulmaTarihi.ToString("dd.MM.yyyy HH:mm");
    }
}
