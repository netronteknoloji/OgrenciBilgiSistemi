namespace OgrenciBilgiSistemi.Api.Models
{
    public class BirimModel
    {
        public int BirimId { get; set; }
        public string BirimAd { get; set; } = string.Empty;
        public bool BirimDurum { get; set; }
        public bool BirimSinifMi { get; set; }
    }
}
