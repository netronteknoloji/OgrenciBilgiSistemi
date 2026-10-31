using OgrenciBilgiSistemi.Data;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    /// <summary>
    /// Yemek hakkının tek tanımı: öğrencinin o takvim ayı için OgrenciYemekler kaydı Aktif'tir.
    /// Ödeme hak vermez. Kart okuma (anlık yol), cihaz senkronu ve yemekhane log okuma bunu kullanır.
    /// </summary>
    public static class YemekHakki
    {
        // Global filtre (silinmiş öğrencinin kaydı hak vermez) açık kalır; IncludeDeleted geçici olarak
        // true yapılmış bir context'te de aynı sonucu vermesi için koşul ayrıca açıkça yazılır.
        public static IQueryable<int> HakkiOlanlar(AppDbContext db, int yil, int ay)
            => db.OgrenciYemekler
                .Where(y => y.Yil == yil && y.Ay == ay && y.Aktif && !y.Ogrenci.IsDeleted)
                .Select(y => y.OgrenciId);
    }
}
