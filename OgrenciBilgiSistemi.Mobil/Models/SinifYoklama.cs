namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class SinifYoklama
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

        /// <summary>
        /// Ders numarasına göre (1-8) yoklama durumunu döner. Reflection kullanımını önler.
        /// </summary>
        public int? DersGetir(int dersNumarasi) => dersNumarasi switch
        {
            1 => Ders1,
            2 => Ders2,
            3 => Ders3,
            4 => Ders4,
            5 => Ders5,
            6 => Ders6,
            7 => Ders7,
            8 => Ders8,
            _ => null
        };
    }
}
