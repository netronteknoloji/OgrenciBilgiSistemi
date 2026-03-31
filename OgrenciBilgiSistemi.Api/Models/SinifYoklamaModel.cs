namespace OgrenciBilgiSistemi.Api.Models
{
    public class SinifYoklamaModel
    {
        public int SinifYoklamaId { get; set; }
        public int OgrenciId { get; set; }
        public int KullaniciId { get; set; }
        public int? Ders1 { get; set; }
        public int? Ders2 { get; set; }
        public int? Ders3 { get; set; }
        public int? Ders4 { get; set; }
        public int? Ders5 { get; set; }
        public int? Ders6 { get; set; }
        public int? Ders7 { get; set; }
        public int? Ders8 { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
    }
}
