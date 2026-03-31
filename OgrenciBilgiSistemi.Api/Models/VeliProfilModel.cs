using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Api.Models
{
    public class VeliProfilModel
    {
        public int KullaniciId { get; set; }
        public string? VeliAdres { get; set; }
        public string? VeliMeslek { get; set; }
        public string? VeliIsYeri { get; set; }
        public string? VeliEmail { get; set; }
        public YakinlikTipi? VeliYakinlik { get; set; }
        public bool VeliDurum { get; set; }
    }
}
