using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public static class ServisRaporSekme
    {
        public const string Soforler = "soforler";
        public const string Ogrenciler = "ogrenciler";
        public const string Yoklama = "yoklama";

        public static string Normalize(string? sekme) => sekme switch
        {
            Ogrenciler => Ogrenciler,
            Yoklama => Yoklama,
            _ => Soforler
        };
    }

    public class ServisRaporVm
    {
        public string Sekme { get; init; } = ServisRaporSekme.Soforler;
        public int? ServisId { get; init; }
        public string? AramaMetni { get; init; }
        public string? StartDate { get; init; }
        public string? EndDate { get; init; }

        // Servis filtresi dropdown'ı
        public List<ServisProfilModel> Servisler { get; init; } = new();

        public List<ServisProfilModel> Soforler { get; init; } = new();
        public List<OgrenciModel> Ogrenciler { get; init; } = new();
        public SayfalanmisListeModel<ServisYoklamaModel>? Yoklamalar { get; init; }
    }
}
