using Microsoft.EntityFrameworkCore;

public class SayfalanmisListeModel<T> : List<T>
{
    public int SayfaIndeks { get; }
    public int ToplamSayfa { get; }
    public int ToplamSayi { get; }

    public int SayfaBoyutu { get; }

    public bool OncekiSayfaVar => SayfaIndeks > 1;
    public bool SonrakiSayfaVar => SayfaIndeks < ToplamSayfa;
    public bool IlkSayfaMi => SayfaIndeks == 1;
    public bool SonSayfaMi => SayfaIndeks == ToplamSayfa;

    private SayfalanmisListeModel(List<T> items, int count, int pageIndex, int pageSize)
    {
        ToplamSayi = count;
        SayfaBoyutu = pageSize;
        ToplamSayfa = count > 0
            ? (int)Math.Ceiling(count / (double)pageSize)
            : 1;

        SayfaIndeks = pageIndex;

        AddRange(items);
    }

    public static async Task<SayfalanmisListeModel<T>> CreateAsync(
        IQueryable<T> source,
        int pageIndex,
        int pageSize,
        CancellationToken ct)
    {
        // Güvenlik: minimum 1
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, pageSize);

        var count = await source.CountAsync(ct);

        // Toplam sayfa hesapla
        var totalPages = count > 0
            ? (int)Math.Ceiling(count / (double)pageSize)
            : 1;

        // İstenilen sayfa, toplam sayfadan büyükse son sayfaya çek
        if (pageIndex > totalPages)
            pageIndex = totalPages;

        var items = await source
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new SayfalanmisListeModel<T>(items, count, pageIndex, pageSize);
    }
}
