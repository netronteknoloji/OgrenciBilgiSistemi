using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Birim
    {
        [JsonPropertyName("birimId")]
        public int Id { get; set; }

        [JsonPropertyName("birimAd")]
        public string Name { get; set; }

        [JsonPropertyName("birimDurum")]
        public bool IsActive { get; set; }

        [JsonPropertyName("birimSinifMi")]
        public bool IsClass { get; set; }
    }
}
