using System.Collections.Generic;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    // CollectionView IsGrouped="True" için List<Ogrenci> türetilmiş grup sınıfı.
    public class OgrenciGrubu : List<Ogrenci>
    {
        public string BaslikAdi { get; }
        public bool KendiSinifi { get; }

        public OgrenciGrubu(string baslikAdi, bool kendiSinifi, IEnumerable<Ogrenci> ogrenciler)
            : base(ogrenciler)
        {
            BaslikAdi = baslikAdi;
            KendiSinifi = kendiSinifi;
        }
    }
}
