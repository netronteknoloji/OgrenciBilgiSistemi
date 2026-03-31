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
    }
}
