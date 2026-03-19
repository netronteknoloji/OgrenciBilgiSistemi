using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Kullanici
    {
        [JsonPropertyName("kullaniciId")]
        public int Id { get; set; }

        [JsonPropertyName("kullaniciAdi")]
        public string Username { get; set; }

        // API'de bu alan yok; login response'undaki özel alandan doldurulur
        public string? FullName { get; set; }

        [JsonPropertyName("adminMi")]
        public bool IsAdmin { get; set; }

        [JsonPropertyName("kullaniciDurum")]
        public bool IsActive { get; set; }

        [JsonPropertyName("birimId")]
        public int? UnitId { get; set; }
    }
}
