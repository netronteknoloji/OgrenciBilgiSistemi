using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    // API'deki SinifYoklamaDurumModel ile eşleşir — yoklama durum tanımları
    public class SinifYoklamaDurum
    {
        [JsonPropertyName("durumId")]
        public int Id { get; set; }

        [JsonPropertyName("durumAd")]
        public string DurumAd { get; set; } = string.Empty;
    }
}
