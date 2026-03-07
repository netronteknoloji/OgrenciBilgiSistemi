namespace OgrenciBilgiSistemi.Api.Models
{
    public class OgretmenDto
    {
        public int OgretmenId { get; set; }
        public string OgretmenAdSoyad { get; set; } = string.Empty;
        public string? OgretmenGorsel { get; set; }
        public bool OgretmenDurum { get; set; }
        public int? BirimId { get; set; }
        public string? OgretmenKartNo { get; set; }
    }
}
