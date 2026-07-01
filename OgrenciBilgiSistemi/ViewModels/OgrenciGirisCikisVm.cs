using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    // Her bir hareket kaydını temsil eden view model
    public class OgrenciGirisCikisVm
    {
        public int OgrenciDetayId { get; set; }
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        public string OgrenciKartNo { get; set; } = string.Empty;
        public DateTime? OgrenciGTarih { get; set; }
        public DateTime? OgrenciCTarih { get; set; }
        public string OgrenciGecisTipi { get; set; } = string.Empty;
        public string CihazAdi { get; set; } = string.Empty;
    }

    // Wrapper model: Öğrenci bilgilerini ve hareketlerinin sayfalı listesini içerir
    public class OgrenciGirisCikisListViewModel
    {
        public OgrenciModel Ogrenci { get; set; }
        public SayfalanmisListeModel<OgrenciGirisCikisVm> Hareketler { get; set; }
        public List<SinifYoklamaModel> SinifYoklamalar { get; set; } = new();
        public List<ServisYoklamaModel> ServisYoklamalar { get; set; } = new();
        public RaporTipi RaporTipi { get; set; } = RaporTipi.Tumu;
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string RaporTipiStr { get; set; } = "0";
    }

    // Global Detay listesi için rapor tipine göre dinamik içerikli wrapper
    public class OgrenciDetayRaporVm
    {
        public RaporTipi RaporTipi { get; set; } = RaporTipi.Tumu;
        public string? CurrentSort { get; set; }
        public string? CurrentFilter { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string RaporTipiStr { get; set; } = "0";
        public SayfalanmisListeModel<OgrenciDetayModel>? Gecisler { get; set; }
        public SayfalanmisListeModel<SinifYoklamaModel>? SinifYoklamalar { get; set; }
        public SayfalanmisListeModel<ServisYoklamaModel>? ServisYoklamalar { get; set; }

        // Görünen aktif sayfanın paging bilgileri (view'da kolay kullanım için)
        public int SayfaIndeks =>
            Gecisler?.SayfaIndeks ?? SinifYoklamalar?.SayfaIndeks ?? ServisYoklamalar?.SayfaIndeks ?? 1;
        public int ToplamSayfa =>
            Gecisler?.ToplamSayfa ?? SinifYoklamalar?.ToplamSayfa ?? ServisYoklamalar?.ToplamSayfa ?? 1;
        public int ToplamSayi =>
            Gecisler?.ToplamSayi ?? SinifYoklamalar?.ToplamSayi ?? ServisYoklamalar?.ToplamSayi ?? 0;
        public bool OncekiSayfaVar =>
            Gecisler?.OncekiSayfaVar ?? SinifYoklamalar?.OncekiSayfaVar ?? ServisYoklamalar?.OncekiSayfaVar ?? false;
        public bool SonrakiSayfaVar =>
            Gecisler?.SonrakiSayfaVar ?? SinifYoklamalar?.SonrakiSayfaVar ?? ServisYoklamalar?.SonrakiSayfaVar ?? false;
    }
}
