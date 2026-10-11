using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Models;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class GecisKarariService : IGecisKarariService
    {
        private readonly IKartOkuService _kartOku;
        private readonly KartOkumaAyarlari _ayar;

        public GecisKarariService(IKartOkuService kartOku, IOptions<KartOkumaAyarlari> ayar)
        {
            _kartOku = kartOku;
            _ayar = ayar.Value;
        }

        public async Task<GecisKarari> KararVerAsync(IstasyonTipi istasyon, OgrenciModel ogrenci, DateTime simdi, CancellationToken ct = default)
        {
            var bugun = simdi.Date;
            var yarin = bugun.AddDays(1);
            var durum = new GecisDurumu(istasyon, ogrenci.OgrenciCikisDurumu, simdi);

            // Yalnızca istasyonun kuralının ihtiyaç duyduğu veriler sorgulanır
            if (istasyon == IstasyonTipi.Yemekhane)
            {
                var izin = await _kartOku.YemekIzniVarMiAsync(ogrenci.OgrenciId, simdi.Year, simdi.Month, ct);
                durum = durum with
                {
                    YemekIzniVar = izin,
                    BugunYemekVar = izin && await _kartOku.BugunYemekGirisiVarMiAsync(ogrenci.OgrenciId, bugun, yarin, ct)
                };
            }
            else if (istasyon == IstasyonTipi.AnaKapi && ogrenci.OgrenciCikisDurumu == OglenCikisDurumu.Evet)
            {
                var (cikisVar, girisVar, cikisZamani) =
                    await _kartOku.BugunAnaKapiHareketleriAsync(ogrenci.OgrenciId, bugun, yarin, ct);
                durum = durum with
                {
                    AnaKapiCikisVar = cikisVar,
                    AnaKapiGirisVar = girisVar,
                    AnaKapiCikisZamani = cikisZamani
                };
            }

            return GecisKurallari.KararVer(durum, _ayar);
        }
    }
}
