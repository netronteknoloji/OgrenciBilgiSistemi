namespace OgrenciBilgiSistemi.Models
{
    public class HataGorunumModel
    {
        public string? IstemKimlik { get; set; }

        public bool IstemKimlikGoster => !string.IsNullOrEmpty(IstemKimlik);
    }
}
