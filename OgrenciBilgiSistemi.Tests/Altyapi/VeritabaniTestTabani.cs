using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Tests.Altyapi
{
    /// <summary>
    /// Her test için izole bir LocalDB veritabanı oluşturur (EnsureCreated) ve test
    /// bitince siler. SQLite yerine LocalDB kullanılır: üretimle aynı sağlayıcı,
    /// decimal aggregate ve Turkish_100_CI_AI collation desteği.
    /// </summary>
    public abstract class VeritabaniTestTabani : IAsyncLifetime
    {
        private readonly string _dbAdi = $"OBS_Test_{Guid.NewGuid():N}";

        protected DbContextOptions<AppDbContext> Options { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            Options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    $@"Server=(localdb)\MSSQLLocalDB;Database={_dbAdi};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
                .Options;

            await using var ctx = YeniContext();
            await ctx.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await using var ctx = YeniContext();
            await ctx.Database.EnsureDeletedAsync();
        }

        protected AppDbContext YeniContext() => new(Options);

        // ---------------- Ortak seed yardımcıları ----------------

        protected static async Task<BirimModel> BirimEkleAsync(AppDbContext ctx, string ad = "5-A")
        {
            var birim = new BirimModel { BirimAd = ad };
            ctx.Birimler.Add(birim);
            await ctx.SaveChangesAsync();
            return birim;
        }

        protected static async Task<OgrenciModel> OgrenciEkleAsync(
            AppDbContext ctx,
            string adSoyad,
            int no,
            int? birimId = null,
            bool aktif = true,
            string? kartNo = null)
        {
            var ogrenci = new OgrenciModel
            {
                OgrenciAdSoyad = adSoyad,
                OgrenciNo = no,
                BirimId = birimId,
                OgrenciDurum = aktif,
                OgrenciKartNo = kartNo
            };
            ctx.Ogrenciler.Add(ogrenci);
            await ctx.SaveChangesAsync();
            return ogrenci;
        }

        protected static async Task TarifeEkleAsync(AppDbContext ctx, int yil, decimal tutar)
        {
            ctx.OgrenciAidatTarifeler.Add(new OgrenciAidatTarifeModel
            {
                BaslangicYil = yil,
                Tutar = tutar
            });
            await ctx.SaveChangesAsync();
        }
    }
}
