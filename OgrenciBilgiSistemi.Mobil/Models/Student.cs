using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Ogrenci
    {
        [JsonPropertyName("ogrenciId")]
        public int Id { get; set; }

        [JsonPropertyName("ogrenciAdSoyad")]
        public string FullName { get; set; }

        [JsonPropertyName("ogrenciNo")]
        public int StudentNumber { get; set; }

        [JsonPropertyName("ogrenciGorsel")]
        public string? ImagePath { get; set; }

        [JsonPropertyName("ogrenciKartNo")]
        public string? CardNumber { get; set; }

        [JsonPropertyName("ogrenciCikisDurumu")]
        public int ExitStatus { get; set; }

        [JsonPropertyName("ogrenciDurum")]
        public bool IsActive { get; set; }

        [JsonPropertyName("birimId")]
        public int? UnitId { get; set; }

        [JsonPropertyName("personelId")]
        public int? PersonnelId { get; set; }

        [JsonPropertyName("ogrenciVeliId")]
        public int? ParentId { get; set; }

        [JsonPropertyName("ogretmenId")]
        public int? TeacherId { get; set; }

        [JsonPropertyName("servisId")]
        public int? ServiceId { get; set; }

        // Mobil'e özel alanlar — API'nin detay endpoint'inden Dictionary olarak doldurulur
        public string? ClassName { get; set; }
        public string? ParentFullName { get; set; }
        public string? ParentPhoneNumber { get; set; }
    }
}
