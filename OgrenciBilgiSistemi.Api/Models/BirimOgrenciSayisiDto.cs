namespace OgrenciBilgiSistemi.Api.Models
{
    public class BirimOgrenciSayisiDto
    {
        public BirimDto Birim { get; set; } = new();
        public int OgrenciSayisi { get; set; }
    }
}
