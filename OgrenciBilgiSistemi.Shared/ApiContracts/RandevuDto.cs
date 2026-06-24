namespace OgrenciBilgiSistemi.Shared.ApiContracts
{
    public class RandevuDto
    {
        public int RandevuId { get; set; }
        public int OgretmenKullaniciId { get; set; }
        public string OgretmenAdSoyad { get; set; } = string.Empty;
        public int VeliKullaniciId { get; set; }
        public string VeliAdSoyad { get; set; } = string.Empty;
        public int? OgrenciId { get; set; }
        public string? OgrenciAdSoyad { get; set; }
        public DateTime RandevuTarihi { get; set; }
        public int SureDakika { get; set; }
        public int Durum { get; set; }
        public string DurumAdi { get; set; } = string.Empty;
        public string? Not { get; set; }
        public bool OgretmenTarafindanOlusturuldu { get; set; }
        public DateTime OlusturulmaTarihi { get; set; }
    }
}
