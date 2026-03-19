using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Ogretmen
    {
        [JsonPropertyName("ogretmenId")]
        public int Id { get; set; }

        [JsonPropertyName("ogretmenAdSoyad")]
        public string FullName { get; set; }

        [JsonPropertyName("ogretmenGorsel")]
        public string? ImagePath { get; set; }

        [JsonPropertyName("ogretmenDurum")]
        public bool IsActive { get; set; }

        [JsonPropertyName("birimId")]
        public int? UnitId { get; set; }

        [JsonPropertyName("ogretmenKartNo")]
        public string? CardNumber { get; set; }
    }
}
