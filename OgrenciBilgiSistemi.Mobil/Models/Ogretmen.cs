namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Ogretmen
    {
        public int PersonelId { get; set; }
        public string PersonelAdSoyad { get; set; }
        public string? PersonelGorselPath { get; set; }
        public bool PersonelDurum { get; set; }
        public int? BirimId { get; set; }
        public string? PersonelKartNo { get; set; }
    }
}
