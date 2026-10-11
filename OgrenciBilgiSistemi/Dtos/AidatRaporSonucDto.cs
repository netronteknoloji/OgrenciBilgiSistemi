namespace OgrenciBilgiSistemi.Dtos
{
    public class AidatRaporSonucDto
    {
        public SayfalanmisListeModel<AidatRaporDto> Satirlar { get; set; } = null!;

        public decimal ToplamBorc { get; set; }
        public decimal ToplamOdenenGosterilen { get; set; }
        public decimal ToplamKalan { get; set; }

        public List<int> KullanilabilirYillar { get; set; } = new();

        // Seçili yılın tarifesi ve tarife değişince etkilenecek (ödemesi olan) öğrenci sayısı
        public TarifeDto? Tarife { get; set; }
        public int OdemesiOlanSayisi { get; set; }
    }
}
