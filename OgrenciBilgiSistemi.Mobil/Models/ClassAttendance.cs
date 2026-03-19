using System.Text.Json.Serialization;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class SinifYoklama
    {
        [JsonPropertyName("sinifYoklamaId")]
        public int AttendanceId { get; set; }

        [JsonPropertyName("ogrenciId")]
        public int StudentId { get; set; }

        [JsonPropertyName("ogretmenId")]
        public int TeacherId { get; set; }

        [JsonPropertyName("ders1")]
        public int? Lesson1 { get; set; }

        [JsonPropertyName("ders2")]
        public int? Lesson2 { get; set; }

        [JsonPropertyName("ders3")]
        public int? Lesson3 { get; set; }

        [JsonPropertyName("ders4")]
        public int? Lesson4 { get; set; }

        [JsonPropertyName("ders5")]
        public int? Lesson5 { get; set; }

        [JsonPropertyName("ders6")]
        public int? Lesson6 { get; set; }

        [JsonPropertyName("ders7")]
        public int? Lesson7 { get; set; }

        [JsonPropertyName("ders8")]
        public int? Lesson8 { get; set; }

        [JsonPropertyName("olusturulmaTarihi")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("guncellenmeTarihi")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Ders numarasına göre (1-8) yoklama durumunu döner. Reflection kullanımını önler.
        /// </summary>
        public int? GetLesson(int lessonNumber) => lessonNumber switch
        {
            1 => Lesson1,
            2 => Lesson2,
            3 => Lesson3,
            4 => Lesson4,
            5 => Lesson5,
            6 => Lesson6,
            7 => Lesson7,
            8 => Lesson8,
            _ => null
        };
    }
}
