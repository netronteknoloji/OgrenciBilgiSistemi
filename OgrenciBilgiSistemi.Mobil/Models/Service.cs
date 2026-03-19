using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Servis
    {
        [JsonPropertyName("servisId")]
        public int Id { get; set; }

        [JsonPropertyName("plaka")]
        public string PlateNumber { get; set; }

        [JsonPropertyName("kullaniciId")]
        public int UserId { get; set; }
    }
}
