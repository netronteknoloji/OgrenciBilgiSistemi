using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class KimlikDogrulamaService : IKimlikDogrulamaService
    {
        private AppDbContext CreateContext(string connectionString)
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new AppDbContext(opts);
        }

        public async Task<KullaniciModel?> DogrulaAsync(
            string connectionString, string kullaniciAdi, string sifre, CancellationToken ct = default)
        {
            await using var db = CreateContext(connectionString);

            var user = await db.Kullanicilar
                .Where(k => k.KullaniciDurum)
                .SingleOrDefaultAsync(u => u.KullaniciAdi == kullaniciAdi, ct);

            if (user is null)
                return null;

            var hasher = new PasswordHasher<KullaniciModel>();
            if (hasher.VerifyHashedPassword(user, user.Sifre, sifre) == PasswordVerificationResult.Failed)
                return null;

            return user;
        }

        public async Task GenelAdminOlusturAsync(string connectionString, CancellationToken ct = default)
        {
            await using var db = CreateContext(connectionString);

            var mevcutKayit = await db.Kullanicilar
                .FirstOrDefaultAsync(k => k.Rol == KullaniciRolu.GenelAdmin, ct);

            if (mevcutKayit != null)
                return;

            var genelAdmin = new KullaniciModel
            {
                KullaniciAdi = "GenelAdmin",
                Sifre = "-",
                Rol = KullaniciRolu.GenelAdmin,
                KullaniciDurum = true
            };
            db.Kullanicilar.Add(genelAdmin);
            await db.SaveChangesAsync(ct);

            var tumMenuler = await db.MenuOgeler
                .Select(m => m.Id)
                .ToListAsync(ct);

            foreach (var menuId in tumMenuler)
            {
                db.KullaniciMenuOgeler.Add(new KullaniciMenuModel
                {
                    KullaniciId = genelAdmin.KullaniciId,
                    MenuOgeId = menuId
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
