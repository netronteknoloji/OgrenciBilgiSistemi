using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    #region Veli Veri Modeli Tanımı
    public class OgrenciVeli
    {
        [JsonPropertyName("ogrenciVeliId")]
        public int Id { get; set; }

        [JsonPropertyName("veliAdSoyad")]
        public string? FullName { get; set; }

        [JsonPropertyName("veliTelefon")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("veliAdres")]
        public string? Address { get; set; }

        [JsonPropertyName("veliMeslek")]
        public string? Occupation { get; set; }

        [JsonPropertyName("veliIsYeri")]
        public string? Workplace { get; set; }

        [JsonPropertyName("veliEmail")]
        public string? Email { get; set; }

        [JsonPropertyName("veliYakinlik")]
        public int? RelationshipType { get; set; }

        [JsonPropertyName("veliDurum")]
        public bool IsActive { get; set; }
    }
    #endregion
}
