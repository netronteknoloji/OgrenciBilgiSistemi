using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ServisDetayVm
    {
        public ServisProfilModel Servis { get; set; } = default!;
        public List<OgrenciModel> Ogrenciler { get; set; } = new();
    }
}
