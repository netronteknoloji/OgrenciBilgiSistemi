using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public bool IncludePasifOgrenciler { get; set; } = false;

        public DbSet<BirimModel> Birimler { get; set; }
        public DbSet<PersonelModel> Personeller { get; set; }
        public DbSet<PersonelDetayModel> PersonelDetaylar { get; set; }
        public DbSet<KullaniciModel> Kullanicilar { get; set; }
        public DbSet<OgrenciModel> Ogrenciler { get; set; }
        public DbSet<OgrenciVeliModel> OgrenciVeliler { get; set; }
        public DbSet<OgrenciDetayModel> OgrenciDetaylar { get; set; }
        public DbSet<KitapModel> Kitaplar { get; set; }
        public DbSet<KitapDetayModel> KitapDetaylar { get; set; }
        public DbSet<CihazModel> Cihazlar { get; set; }
        public DbSet<MenuOgeModel> MenuOgeler { get; set; }
        public DbSet<KullaniciMenuModel> KullaniciMenuOgeler { get; set; }
        public DbSet<OgrenciAidatModel> OgrenciAidatlar { get; set; }
        public DbSet<OgrenciAidatTarifeModel> OgrenciAidatTarifeler { get; set; }
        public DbSet<OgrenciAidatOdemeModel> OgrenciAidatOdemeler { get; set; }
        public DbSet<OgrenciYemekModel> OgrenciYemekler { get; set; }
        public DbSet<OgrenciYemekTarifeModel> OgrenciYemekTarifeler { get; set; }
        public DbSet<OgrenciYemekOdemeModel> OgrenciYemekOdemeler { get; set; }
        public DbSet<ZiyaretciModel> Ziyaretciler { get; set; }
        public DbSet<ServisModel> Servisler { get; set; }
        public DbSet<SinifYoklamaModel> SinifYoklamalar { get; set; }
        public DbSet<SinifYoklamaDurumModel> SinifYoklamaDurumlar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // GLOBAL QUERY FILTER
            // =========================
            modelBuilder.Entity<OgrenciModel>()
                .HasQueryFilter(o => o.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // OGRENCI <-> PERSONEL (optional)
            // =========================
            modelBuilder.Entity<PersonelModel>()
                .HasMany(p => p.Ogrenciler)
                .WithOne(o => o.Personel)
                .HasForeignKey(o => o.PersonelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OgrenciModel>()
                .HasOne(o => o.OgrenciVeli)
                .WithMany(v => v.Ogrenciler)
                .HasForeignKey(o => o.OgrenciVeliId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================
            // OGRENCI <-> BIRIM (optional)
            // =========================
            modelBuilder.Entity<OgrenciModel>()
                .HasOne(o => o.Birim)
                .WithMany(b => b.Ogrenciler)
                .HasForeignKey(o => o.BirimId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // =========================
            // OGRENCI DETAYLAR
            // =========================
            modelBuilder.Entity<OgrenciDetayModel>()
                .HasOne(d => d.Ogrenci)
                .WithMany(o => o.OgrenciDetaylar)
                .HasForeignKey(d => d.OgrenciId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OgrenciDetayModel>()
                .HasOne(d => d.Cihaz)
                .WithMany()
                .HasForeignKey(d => d.CihazId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OgrenciDetayModel>()
                .HasQueryFilter(d => d.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // KITAP DETAY (matching filter + ilişkiyi netleştir)
            // =========================
            modelBuilder.Entity<KitapDetayModel>()
                .HasOne(k => k.Ogrenci)
                .WithMany() // Ogrenci tarafında ayrı koleksiyon yok
                .HasForeignKey(k => k.OgrenciId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            modelBuilder.Entity<KitapDetayModel>()
                .HasQueryFilter(k => k.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // YEMEK: KAYIT (required -> Ogrenci)
            // =========================
            modelBuilder.Entity<OgrenciYemekModel>(b =>
            {
                b.HasOne(y => y.Ogrenci)
                 .WithMany(o => o.OgrenciYemekler)
                 .HasForeignKey(y => y.OgrenciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                b.HasIndex(y => new { y.OgrenciId, y.Yil, y.Ay }).IsUnique();
            });

            modelBuilder.Entity<OgrenciYemekModel>()
                .HasQueryFilter(y => y.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // YEMEK: TARIFE (required -> Ogrenci)
            // =========================
            modelBuilder.Entity<OgrenciYemekTarifeModel>(e =>
            {
                e.HasOne(t => t.Ogrenci)
                 .WithMany()
                 .HasForeignKey(t => t.OgrenciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(x => new { x.OgrenciId, x.Yil }).IsUnique();
            });

            modelBuilder.Entity<OgrenciYemekTarifeModel>()
                .HasQueryFilter(t => t.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // YEMEK: ODEME (required -> Ogrenci)
            // =========================
            modelBuilder.Entity<OgrenciYemekOdemeModel>(e =>
            {
                e.HasOne(p => p.Ogrenci)
                 .WithMany()
                 .HasForeignKey(p => p.OgrenciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(x => new { x.OgrenciId, x.Yil, x.Ay }); // gerekirse .IsUnique()
            });

            modelBuilder.Entity<OgrenciYemekOdemeModel>()
                .HasQueryFilter(p => p.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // AIDAT: KAYIT (required -> Ogrenci)
            // =========================
            modelBuilder.Entity<OgrenciAidatModel>(e =>
            {
                e.HasOne(a => a.Ogrenci)
                 .WithMany(o => o.OgrenciAidatlar)
                 .HasForeignKey(a => a.OgrenciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(x => new { x.OgrenciId, x.BaslangicYil }).IsUnique();

                e.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Aidat_BaslangicYil", "[BaslangicYil] BETWEEN 2000 AND 2100");
                    tb.HasCheckConstraint("CK_Aidat_Pozitif", "[Borc] >= 0 AND [Odenen] >= 0");
                });
            });

            modelBuilder.Entity<OgrenciAidatModel>()
                .HasQueryFilter(a => a.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // AIDAT: ODEME (required -> Aidat -> Ogrenci)
            // =========================
            modelBuilder.Entity<OgrenciAidatOdemeModel>(e =>
            {
                e.HasOne(x => x.OgrenciAidat)
                 .WithMany(a => a.Odemeler)
                 .HasForeignKey(x => x.OgrenciAidatId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(x => new { x.OgrenciAidatId, x.OdemeTarihi });

                e.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_AidatOdeme_Tutar_NonNegative", "[Tutar] >= 0");
                });
            });

            modelBuilder.Entity<OgrenciAidatOdemeModel>()
                .HasQueryFilter(x => x.OgrenciAidat.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // AIDAT: TARIFE (global)
            // =========================
            modelBuilder.Entity<OgrenciAidatTarifeModel>(e =>
            {
                e.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Tarife_BaslangicYil", "[BaslangicYil] BETWEEN 2000 AND 2100");
                    tb.HasCheckConstraint("CK_Tarife_Tutar", "[Tutar] >= 0");
                });
            });

            // =========================
            // OGRENCI <-> SERVIS (optional)
            // =========================
            modelBuilder.Entity<OgrenciModel>()
                .HasOne(o => o.Servis)
                .WithMany(s => s.Ogrenciler)
                .HasForeignKey(o => o.ServisId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================
            // SERVIS <-> KULLANICI (optional)
            // =========================
            modelBuilder.Entity<ServisModel>()
                .HasOne(s => s.Kullanici)
                .WithMany()
                .HasForeignKey(s => s.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================
            // SINIF YOKLAMA
            // =========================
            modelBuilder.Entity<SinifYoklamaModel>(e =>
            {
                e.HasOne(sy => sy.Ogrenci)
                 .WithMany(o => o.SinifYoklamalar)
                 .HasForeignKey(sy => sy.OgrenciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasOne(sy => sy.Personel)
                 .WithMany(p => p.SinifYoklamalar)
                 .HasForeignKey(sy => sy.PersonelId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(sy => new { sy.OgrenciId, sy.OlusturulmaTarihi });
            });

            modelBuilder.Entity<SinifYoklamaModel>()
                .HasQueryFilter(sy => sy.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // SINIF YOKLAMA DURUM (Seed Data)
            // =========================
            modelBuilder.Entity<SinifYoklamaDurumModel>().HasData(
                new SinifYoklamaDurumModel { DurumId = 1, DurumAd = "Var" },
                new SinifYoklamaDurumModel { DurumId = 2, DurumAd = "Yok" },
                new SinifYoklamaDurumModel { DurumId = 3, DurumAd = "Geç" },
                new SinifYoklamaDurumModel { DurumId = 4, DurumAd = "İzinli" },
                new SinifYoklamaDurumModel { DurumId = 5, DurumAd = "Raporlu" }
            );

            // =========================
            // UNIQUE INDEKSLER
            // =========================

            // Öğrenci numarası — her numara yalnızca bir öğrenciye ait olabilir
            modelBuilder.Entity<OgrenciModel>()
                .HasIndex(o => o.OgrenciNo)
                .IsUnique()
                .HasDatabaseName("UX_Ogrenciler_OgrenciNo");

            // Kart numarası — bir kart birden fazla öğrenciye atanamaz
            // Boş/null değerler benzersizlik kapsamı dışında tutulur
            modelBuilder.Entity<OgrenciModel>()
                .HasIndex(o => o.OgrenciKartNo)
                .IsUnique()
                .HasFilter("[OgrenciKartNo] IS NOT NULL AND [OgrenciKartNo] != ''")
                .HasDatabaseName("UX_Ogrenciler_OgrenciKartNo");

            // Kullanıcı adı — tüm kayıtlar arasında benzersiz olmalı
            modelBuilder.Entity<KullaniciModel>()
                .HasIndex(k => k.KullaniciAdi)
                .IsUnique()
                .HasDatabaseName("UX_Kullanicilar_KullaniciAdi");

            // =========================
            // KULLANICI-MENU (M:N)
            // =========================
            modelBuilder.Entity<KullaniciMenuModel>()
                .HasKey(x => new { x.KullaniciId, x.MenuOgeId });

            modelBuilder.Entity<KullaniciMenuModel>()
                .HasOne(x => x.Kullanici)
                .WithMany(k => k.KullaniciMenuler)
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KullaniciMenuModel>()
                .HasOne(x => x.MenuOge)
                .WithMany(m => m.KullaniciMenuler)
                .HasForeignKey(x => x.MenuOgeId)
                .OnDelete(DeleteBehavior.Cascade);

            // *** KÖPRÜYE SEED YOK *** (FK hatası yaşamamak için)
            // Kullanıcı-menü atamalarını UI veya Program.cs'de runtime olarak ekleyebilirsin.

            // =========================
            // MENU HİYERARŞİSİ + SEED
            // =========================
            modelBuilder.Entity<MenuOgeModel>()
                .HasOne(m => m.AnaMenu)
                .WithMany(m => m.AltMenuler)
                .HasForeignKey(m => m.AnaMenuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuOgeModel>()
                .HasIndex(m => new { m.AnaMenuId, m.Sirala });

            modelBuilder.Entity<MenuOgeModel>().HasData(
    new MenuOgeModel { Id = 1, Baslik = "Ana Sayfa", Controller = "Home", Action = "Index", AnaMenuId = null, Sirala = 1 },

    new MenuOgeModel { Id = 2, Baslik = "Personeller", Controller = null, Action = null, AnaMenuId = null, Sirala = 2 },
    new MenuOgeModel { Id = 3, Baslik = "Birim Listesi", Controller = "Birimler", Action = "Index", AnaMenuId = 2, Sirala = 1 },
    new MenuOgeModel { Id = 4, Baslik = "Personel Listesi", Controller = "Personeller", Action = "Index", AnaMenuId = 2, Sirala = 2 },

    new MenuOgeModel { Id = 5, Baslik = "Öğrenciler", Controller = null, Action = null, AnaMenuId = null, Sirala = 3 },
    new MenuOgeModel { Id = 6, Baslik = "Öğrenci İşlemleri", Controller = "Ogrenciler", Action = "Index", AnaMenuId = 5, Sirala = 1 },
    new MenuOgeModel { Id = 7, Baslik = "Aidat İşlemleri", Controller = "OgrenciAidat", Action = "Index", AnaMenuId = 5, Sirala = 2 },
    new MenuOgeModel { Id = 8, Baslik = "Yemekhane İşlemleri", Controller = "OgrenciYemekhane", Action = "Index", AnaMenuId = 5, Sirala = 3 },

    new MenuOgeModel { Id = 9, Baslik = "Ziyaretçiler", Controller = null, Action = null, AnaMenuId = null, Sirala = 4 },
    new MenuOgeModel { Id = 10, Baslik = "Ziyaretçi İşlemleri", Controller = "Ziyaretciler", Action = "Index", AnaMenuId = 9, Sirala = 1 },

    new MenuOgeModel { Id = 11, Baslik = "Kullanıcılar", Controller = null, Action = null, AnaMenuId = null, Sirala = 5 },
    new MenuOgeModel { Id = 12, Baslik = "Kullanıcı Listesi", Controller = "Kullanicilar", Action = "Index", AnaMenuId = 11, Sirala = 1 },

    new MenuOgeModel { Id = 13, Baslik = "Kitaplar", Controller = null, Action = null, AnaMenuId = null, Sirala = 6 },
    new MenuOgeModel { Id = 14, Baslik = "Kitap Listesi", Controller = "Kitaplar", Action = "Index", AnaMenuId = 13, Sirala = 1 },
    new MenuOgeModel { Id = 15, Baslik = "Kitap Hareketleri", Controller = "KitapDetaylar", Action = "Index", AnaMenuId = 13, Sirala = 2 },

    new MenuOgeModel { Id = 16, Baslik = "Cihazlar", Controller = null, Action = null, AnaMenuId = null, Sirala = 7 },
    new MenuOgeModel { Id = 17, Baslik = "Cihaz Listesi", Controller = "Cihazlar", Action = "Index", AnaMenuId = 16, Sirala = 1 },

    new MenuOgeModel { Id = 18, Baslik = "Raporlar", Controller = null, Action = null, AnaMenuId = null, Sirala = 8 },
    new MenuOgeModel { Id = 19, Baslik = "Öğrenci Giriş Çıkış Raporları", Controller = "OgrenciGirisCikis", Action = "Detay", AnaMenuId = 18, Sirala = 1 },
    new MenuOgeModel { Id = 20, Baslik = "Öğrenci Veli Raporu", Controller = "Ogrenciler", Action = "OgrenciVeliRapor", AnaMenuId = 18, Sirala = 2 },
    new MenuOgeModel { Id = 21, Baslik = "Öğrenci Aidat Raporu", Controller = "OgrenciAidat", Action = "AidatRapor", AnaMenuId = 18, Sirala = 3 },
    new MenuOgeModel { Id = 22, Baslik = "Öğrenci Ziyaretçi Raporu", Controller = "Ziyaretciler", Action = "ZiyaretciRapor", AnaMenuId = 18, Sirala = 4 },
    new MenuOgeModel { Id = 23, Baslik = "Öğrenci Yemek Raporu", Controller = "OgrenciYemekhane", Action = "YemekRapor", AnaMenuId = 18, Sirala = 5 },

    new MenuOgeModel { Id = 24, Baslik = "KartOku", Controller = null, Action = null, AnaMenuId = null, Sirala = 9 },
    new MenuOgeModel { Id = 25, Baslik = "Kart Okuma Ekranı", Controller = "KartOku", Action = "Index", AnaMenuId = 24, Sirala = 1 }
);
        }
    }
}