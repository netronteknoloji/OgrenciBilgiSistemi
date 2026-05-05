namespace OgrenciBilgiSistemi.Api.Models
{
    public class OkulOzetModel
    {
        public string OkulAdi { get; set; } = string.Empty;
        public int ToplamOgrenci { get; set; }
        public int ToplamOgretmen { get; set; }
        public int ToplamSinif { get; set; }
        public int ToplamVeli { get; set; }
        public int ToplamServis { get; set; }
        public int BugunYemekhaneGiris { get; set; }
        public int BugunAnakapiCikis { get; set; }
    }
}
