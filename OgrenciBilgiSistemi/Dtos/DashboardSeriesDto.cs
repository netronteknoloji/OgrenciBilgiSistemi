namespace OgrenciBilgiSistemi.Dtos
{
    public class DashboardSeriesDto
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public List<string> GunEtiketleri { get; set; } = new();
        public List<int> YemekhaneGiris { get; set; } = new();
        public List<int> AnakapiCikis { get; set; } = new();
    }
}
