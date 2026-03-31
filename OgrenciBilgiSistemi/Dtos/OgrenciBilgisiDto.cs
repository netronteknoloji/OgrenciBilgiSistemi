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
    }
}
