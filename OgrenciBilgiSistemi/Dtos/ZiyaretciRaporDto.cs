namespace OgrenciBilgiSistemi.Dtos
{
    public sealed class ZiyaretciRaporDto
    {
        public int ZiyaretciId { get; set; }

        // --- Ziyaretçi Kimlik Bilgileri ---
        public string AdSoyad { get; set; } = string.Empty;
        public string? TcKimlikNo { get; set; }
        public string? Telefon { get; set; }
        public string? Adres { get; set; }

        // --- Görüştüğü Kullanıcı Bilgisi ---
        public int? KullaniciId { get; set; }
        public string? KullaniciAdi { get; set; }

        // Kullanıcı üzerinden gelen birim adı
        public string? BirimAd { get; set; }

        // --- Ziyaret Bilgileri ---
        public string? ZiyaretSebebi { get; set; }

        // --- Kart Bilgisi ---
        public string? KartNo { get; set; }
        public bool KartVerildiMi { get; set; }

        // --- Giriş / Çıkış ---
        public DateTime GirisZamani { get; set; }
        public DateTime? CikisZamani { get; set; }

        // Hesaplanan süre örnek: "01:25"
        public string? SureText { get; set; }

    }
}