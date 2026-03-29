using Microsoft.EntityFrameworkCore;
using ZKTecoWindowsService.Models;

namespace ZKTecoWindowsService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<OgrenciDetayModel> OgrenciDetaylar { get; set; }

        public DbSet<CihazModel> Cihazlar { get; set; }

        public DbSet<OgrenciModel> Ogrenciler { get; set; }

        public DbSet<OgrenciYemekModel> OgrenciYemekler { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<BirimModel>();
            modelBuilder.Ignore<OgretmenModel>();
            modelBuilder.Ignore<KullaniciModel>();
            modelBuilder.Ignore<KullaniciMenuModel>();
            modelBuilder.Ignore<MenuOgeModel>();
        }
    }
}