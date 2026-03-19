using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    // API'deki GecisKayitModel ile eşleşir — öğrenci giriş/çıkış kayıtları
    public class GecisKayit
    {
        [JsonPropertyName("ogrenciDetayId")]
        public int Id { get; set; }

        [JsonPropertyName("ogrenciId")]
        public int OgrenciId { get; set; }

        [JsonPropertyName("ogrenciAdSoyad")]
        public string OgrenciAdSoyad { get; set; } = string.Empty;

        [JsonPropertyName("ogrenciKartNo")]
        public string? OgrenciKartNo { get; set; }

        [JsonPropertyName("birimAd")]
        public string? BirimAd { get; set; }

        [JsonPropertyName("ogrenciGTarih")]
        public DateTime? GirisTarihi { get; set; }

        [JsonPropertyName("ogrenciCTarih")]
        public DateTime? CikisTarihi { get; set; }

        // "GİRİŞ" veya "ÇIKIŞ" değeri alır
        [JsonPropertyName("ogrenciGecisTipi")]
        public string? GecisTipi { get; set; }

        [JsonPropertyName("istasyonTipi")]
        public int IstasyonTipi { get; set; }

        [JsonPropertyName("cihazAdi")]
        public string? CihazAdi { get; set; }
    }
}
