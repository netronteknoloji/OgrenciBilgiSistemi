namespace OgrenciBilgiSistemi.Api.Models
{
    public class BirimOgrenciSayisiModel
    {
        public BirimModel Birim { get; set; } = new();
        public int OgrenciSayisi { get; set; }
    }
}
