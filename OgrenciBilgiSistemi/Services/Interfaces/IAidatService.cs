using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OgrenciBilgiSistemi.Dtos;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    /// <summary>
    /// Aidat (yıllık ücret) yönetimi servis sözleşmesi.
    /// </summary>
    public interface IAidatService
    {
        /// <summary>Belirli bir öğrenci ve akademik yıl için aidat özetini getirir.</summary>
        Task<OgrenciAidatDto> GetOgrenciAidatAsync(
            int ogrenciId,
            int yil,
            CancellationToken ct = default);

        /// <summary>Filtreli aidat raporunu (sayfalı) döndürür.</summary>
        Task<AidatRaporSonucDto> GetAidatRaporAsync(
            int? yil,
            DateTime? bas,
            DateTime? bit,
            string? query,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            int? tarifeYil = null,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default);


        /// <summary>Bir ödeme satırı ekler ve eklenen satırı döndürür.</summary>
        Task<OdemeSatiriDto> OdemeEkleAsync(
            AidatOdemeEkleDto dto,
            CancellationToken ct = default);

        /// <summary>Ödeme satırını siler.</summary>
        Task<bool> OdemeSilAsync(
            int ogrenciAidatOdemeId,
            CancellationToken ct = default);

        /// <summary>Öğrenciye ait tüm akademik başlangıç yılları.</summary>
        Task<List<int>> GetKullanilabilirYillarAsync(
            int ogrenciId,
            CancellationToken ct = default);

        /// <summary>Belirtilen akademik yıl için tarife kaydeder/günceller.</summary>
        Task<TarifeDto> TarifeKaydetAsync(
            TarifeDto dto,
            CancellationToken ct = default);

        /// <summary>Filtrelerle üretilen aidat raporunu Excel (CSV) olarak dışa aktarır.</summary>
        /// <returns>(içerik, dosyaAdı, içerikTürü)</returns>
        Task<(byte[] Content, string FileName, string ContentType)> ExportAidatRaporExcelAsync(
            int? yil,
            DateTime? bas,
            DateTime? bit,
            string? query,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            int? tarifeYil = null,
            bool includePasif = false,   // <- eklendi
            CancellationToken ct = default);

        /// <summary>Öğrencinin belirli bir yıl için muafiyet durumunu ayarlar.</summary>
        Task<bool> SetYillikMuafiyetAsync(
            int ogrenciId,
            int yil,
            bool muaf,
            CancellationToken ct = default);

        /// <summary>Öğrencinin belirli bir yıl için muafiyet durumunu döndürür.</summary>
        Task<bool> GetYillikMuafiyetAsync(
            int ogrenciId,
            int yil,
            CancellationToken ct = default);
    }
}