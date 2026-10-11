using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Dtos
{
    public class OgrenciBilgisiDto
    {
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        public int OgrenciNo { get; set; }
        public string OgrenciSinif { get; set; } = "-";
        public string OgrenciGorsel { get; set; } = string.Empty;
        public string OgrenciGirisSaati { get; set; } = "-";
        public string OgrenciCikisSaati { get; set; } = "-";
        public OglenCikisDurumu OglenCikisDurumu { get; set; }
        public string GecisTipi { get; set; } = string.Empty;
        public string Istasyon { get; set; } = string.Empty;
        public string CihazAdi { get; set; } = string.Empty;

        public Guid? CihazKodu { get; set; }  
        public string? Reason { get; set; }  
        public string? Error { get; set; }
        public string? Info { get; set; }

        /// <summary>Kart okuma ekranına gidecek sonuç; öğrenci ve cihaz alanları ortak doldurulur.</summary>
        public static OgrenciBilgisiDto Olustur(
            OgrenciModel ogrenci, CihazModel cihaz, string gecisTipi,
            string? reason, string? error = null, string? info = null,
            string girisSaati = "-", string cikisSaati = "-") => new()
        {
            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
            OgrenciNo = ogrenci.OgrenciNo,
            OgrenciSinif = ogrenci.Birim?.BirimAd ?? "-",
            OgrenciGorsel = ogrenci.OgrenciGorsel ?? string.Empty,
            OgrenciGirisSaati = girisSaati,
            OgrenciCikisSaati = cikisSaati,
            OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
            GecisTipi = gecisTipi,
            Istasyon = cihaz.IstasyonTipi.ToString(),
            CihazAdi = cihaz.CihazAdi,
            CihazKodu = cihaz.CihazKodu,
            Reason = reason,
            Error = error,
            Info = info
        };
    }
}
