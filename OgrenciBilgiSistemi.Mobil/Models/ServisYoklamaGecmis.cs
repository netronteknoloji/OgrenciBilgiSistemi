using System;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class ServisYoklamaGecmis
    {
        public int OgrenciId { get; set; }
        public int Periyot { get; set; }     // 1 = Sabah, 2 = Akşam
        public int DurumId { get; set; }     // 1 = Bindi, 2 = Binmedi
        public DateTime Tarih { get; set; }  // OlusturulmaTarihi
    }
}
