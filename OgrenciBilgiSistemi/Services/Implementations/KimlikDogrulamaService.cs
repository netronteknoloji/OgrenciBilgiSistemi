using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Kurallar;

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
                .Include(k => k.OgretmenProfil)
                .Where(k => !k.IsDeleted)
                .SingleOrDefaultAsync(u => u.KullaniciAdi == kullaniciAdi, ct);

            // OturumDogrulayici ile aynı kural (GenelAdmin satırı, pasif öğretmen profili)
            if (user is null || !PanelOturumKurallari.HesapAktifMi(
                    user.Rol, user.IsDeleted, user.OgretmenProfil?.IsDeleted == true))
                return null;

            var hasher = new PasswordHasher<KullaniciModel>();
            if (hasher.VerifyHashedPassword(user, user.Sifre, sifre) == PasswordVerificationResult.Failed)
                return null;

            return user;
        }

        public async Task GenelAdminOlusturAsync(string connectionString, CancellationToken ct = default)
        {
            await using var db = CreateContext(connectionString);

            var genelAdmin = await db.Kullanicilar
                .FirstOrDefaultAsync(k => k.Rol == KullaniciRolu.GenelAdmin, ct);

            if (genelAdmin is null)
            {
                genelAdmin = new KullaniciModel
                {
                    KullaniciAdi = "GenelAdmin",
                    Sifre = "-",
                    Rol = KullaniciRolu.GenelAdmin,
                    IsDeleted = false
                };
                db.Kullanicilar.Add(genelAdmin);
                await db.SaveChangesAsync(ct);
            }
            else if (genelAdmin.IsDeleted)
            {
                genelAdmin.IsDeleted = false;
            }

            // Her okul seçiminde eksik menüler tamamlanır (sonradan eklenen menüler dahil)
            var mevcutMenuler = await db.KullaniciMenuOgeler
                .Where(km => km.KullaniciId == genelAdmin.KullaniciId)
                .Select(km => km.MenuOgeId)
                .ToListAsync(ct);

            var eksikMenuler = await db.MenuOgeler
                .Where(m => !mevcutMenuler.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync(ct);

            foreach (var menuId in eksikMenuler)
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
