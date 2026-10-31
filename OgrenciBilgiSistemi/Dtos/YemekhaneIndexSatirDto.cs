namespace OgrenciBilgiSistemi.Dtos
{
    public enum YemekHakkiFiltresi
    {
        Hepsi = 0,
        Var = 1,
        Yok = 2
    }

    public class YemekhaneIndexSatirDto
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = "";
        public string? OgrenciNo { get; set; }
        public string? OgrenciSinif { get; set; }

        // Seçilen ayda yemek hakkı (kayıt yoksa false)
        public bool HakVar { get; set; }
        public string? Not { get; set; }
    }

    public class YemekHakkiIceAktarmaSonucDto
    {
        // Dolu ise hiçbir değişiklik yapılmadı
        public string? Hata { get; set; }

        public int Acilan { get; set; }
        public int Kapatilan { get; set; }
        public int Degismeyen { get; set; }

        public IReadOnlyList<int> Bulunamayan { get; set; } = [];
        public IReadOnlyList<int> BirdenFazla { get; set; } = [];
        public IReadOnlyList<int> GecersizSatirlar { get; set; } = [];
    }

    public class YemekhaneAylikTakipSonucDto
    {
        public SayfalanmisListeModel<YemekhaneIndexSatirDto> Satirlar { get; set; } = null!;

        // Durum filtresinden önceki sayılar
        public int HakVarSayisi { get; set; }
        public int HakYokSayisi { get; set; }
    }
}
