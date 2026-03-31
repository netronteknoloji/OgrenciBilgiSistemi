using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public bool IncludePasifOgrenciler { get; set; } = false;

        public DbSet<BirimModel> Birimler { get; set; }
        public DbSet<KullaniciModel> Kullanicilar { get; set; }
        public DbSet<OgrenciModel> Ogrenciler { get; set; }
        public DbSet<VeliProfilModel> VeliProfiller { get; set; }
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
        public DbSet<SinifYoklamaModel> SinifYoklamalar { get; set; }

        public DbSet<ServisProfilModel> ServisProfiller { get; set; }
        public DbSet<OgretmenProfilModel> OgretmenProfiller { get; set; }
        public DbSet<ServisYoklamaModel> ServisYoklamalar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // GLOBAL QUERY FILTER
            // =========================
            modelBuilder.Entity<OgrenciModel>()
                .HasQueryFilter(o => o.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // OGRENCI <-> KULLANICI/OGRETMEN (optional)
            // =========================
            modelBuilder.Entity<KullaniciModel>()
                .HasMany(k => k.Ogrenciler)
                .WithOne(o => o.Ogretmen)
                .HasForeignKey(o => o.OgretmenId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OgrenciModel>()
                .HasOne(o => o.Veli)
                .WithMany()
                .HasForeignKey(o => o.VeliId)
                .OnDelete(DeleteBehavior.Restrict);

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
                .HasOne(k => k.Kitap)
                .WithMany()
                .HasForeignKey(k => k.KitapId)
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
                .HasQueryFilter(p => p.AktifMi && (p.Ogrenci.OgrenciDurum || IncludePasifOgrenciler));

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
                .HasQueryFilter(x => x.AktifMi && (x.OgrenciAidat.Ogrenci.OgrenciDurum || IncludePasifOgrenciler));

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
            // OGRENCI <-> KULLANICI/SERVIS (optional)
            // =========================            
            modelBuilder.Entity<OgrenciModel>()
                .HasOne(o => o.ServisKullanici)
                .WithMany()
                .HasForeignKey(o => o.ServisId)
                .OnDelete(DeleteBehavior.Restrict); 

            // =========================
            // KULLANICI <-> VELIPROFIL (1:1)
            // =========================
            modelBuilder.Entity<VeliProfilModel>()
                .HasKey(v => v.KullaniciId);

            modelBuilder.Entity<KullaniciModel>()
                .HasOne(k => k.VeliProfil)
                .WithOne(v => v.Kullanici)
                .HasForeignKey<VeliProfilModel>(v => v.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // KULLANICI <-> SERVISPROFIL (1:1)
            // =========================
            modelBuilder.Entity<ServisProfilModel>()
                .HasKey(s => s.KullaniciId);

            modelBuilder.Entity<KullaniciModel>()
                .HasOne(k => k.ServisProfil)
                .WithOne(s => s.Kullanici)
                .HasForeignKey<ServisProfilModel>(s => s.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // KULLANICI <-> OGRETMENPROFIL (1:1)
            // =========================
            modelBuilder.Entity<OgretmenProfilModel>()
                .HasKey(o => o.KullaniciId);

            modelBuilder.Entity<KullaniciModel>()
                .HasOne(k => k.OgretmenProfil)
                .WithOne(o => o.Kullanici)
                .HasForeignKey<OgretmenProfilModel>(o => o.KullaniciId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // OGRETMENPROFIL <-> BIRIM (optional)
            // =========================
            modelBuilder.Entity<OgretmenProfilModel>()
                .HasOne(o => o.Birim)
                .WithMany()
                .HasForeignKey(o => o.BirimId)
                .OnDelete(DeleteBehavior.SetNull);

            // =========================
            // ZIYARETCI <-> KULLANICI (optional)
            // =========================
            modelBuilder.Entity<ZiyaretciModel>()
                .HasOne(z => z.Kullanici)
                .WithMany(k => k.Ziyaretciler)
                .HasForeignKey(z => z.KullaniciId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ZiyaretciModel>()
                .HasOne(z => z.Cihaz)
                .WithMany()
                .HasForeignKey(z => z.CihazId)
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

                e.HasOne(sy => sy.Kullanici)
                 .WithMany(k => k.SinifYoklamalar)
                 .HasForeignKey(sy => sy.KullaniciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(sy => new { sy.OgrenciId, sy.OlusturulmaTarihi });
            });

            modelBuilder.Entity<SinifYoklamaModel>()
                .HasQueryFilter(sy => sy.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

            // =========================
            // SERVIS YOKLAMA
            // =========================
            modelBuilder.Entity<ServisYoklamaModel>(e =>
            {
                e.HasOne(sy => sy.Ogrenci)
                 .WithMany(o => o.ServisYoklamalar)
                 .HasForeignKey(sy => sy.OgrenciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasOne(sy => sy.Kullanici)
                 .WithMany(k => k.ServisYoklamalar)
                 .HasForeignKey(sy => sy.KullaniciId)
                 .OnDelete(DeleteBehavior.Restrict)
                 .IsRequired();

                e.HasIndex(sy => new { sy.KullaniciId, sy.OgrenciId, sy.Periyot, sy.OlusturulmaTarihi });
            });

            modelBuilder.Entity<ServisYoklamaModel>()
                .HasQueryFilter(sy => sy.Ogrenci.OgrenciDurum || IncludePasifOgrenciler);

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

            //// Kullanıcı adı — tüm kayıtlar arasında benzersiz olmalı
            //modelBuilder.Entity<KullaniciModel>()
            //    .HasIndex(k => k.KullaniciAdi)
            //    .IsUnique()
            //    .HasDatabaseName("UX_Kullanicilar_KullaniciAdi");

            // =========================
            // PERFORMANS INDEKSLERI
            // =========================

            // Rol bazlı yetkilendirme sorgularında kullanılır
            modelBuilder.Entity<KullaniciModel>()
                .HasIndex(k => k.Rol)
                .HasDatabaseName("IX_Kullanicilar_Rol");

            // Sınıfa göre öğrenci filtreleme sorgularında kullanılır
            modelBuilder.Entity<OgrenciModel>()
                .HasIndex(o => o.BirimId)
                .HasDatabaseName("IX_Ogrenciler_BirimId");

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
                new MenuOgeModel { Id = 2, Baslik = "Öğretmenler", Controller = null, Action = null, AnaMenuId = null, Sirala = 2 },
                new MenuOgeModel { Id = 3, Baslik = "Birim İşlemleri", Controller = "Birimler", Action = "Index", AnaMenuId = 2, Sirala = 1 },
                new MenuOgeModel { Id = 4, Baslik = "Öğretmen İşlemleri", Controller = "Ogretmenler", Action = "Index", AnaMenuId = 2, Sirala = 2 },
                new MenuOgeModel { Id = 5, Baslik = "Öğrenciler", Controller = null, Action = null, AnaMenuId = null, Sirala = 3 },
                new MenuOgeModel { Id = 6, Baslik = "Öğrenci İşlemleri", Controller = "Ogrenciler", Action = "Index", AnaMenuId = 5, Sirala = 1 },
                new MenuOgeModel { Id = 7, Baslik = "Aidat İşlemleri", Controller = "Aidat", Action = "Index", AnaMenuId = 5, Sirala = 2 },
                new MenuOgeModel { Id = 8, Baslik = "Yemekhane İşlemleri", Controller = "Yemekhane", Action = "Index", AnaMenuId = 5, Sirala = 3 },
                new MenuOgeModel { Id = 9, Baslik = "Ziyaretçiler", Controller = null, Action = null, AnaMenuId = null, Sirala = 4 },
                new MenuOgeModel { Id = 10, Baslik = "Ziyaretçi İşlemleri", Controller = "Ziyaretciler", Action = "Index", AnaMenuId = 9, Sirala = 1 },
                new MenuOgeModel { Id = 11, Baslik = "Kullanıcılar", Controller = null, Action = null, AnaMenuId = null, Sirala = 5 },
                new MenuOgeModel { Id = 12, Baslik = "Kullanıcı İşlemleri", Controller = "Kullanicilar", Action = "Index", AnaMenuId = 11, Sirala = 1 },
                new MenuOgeModel { Id = 13, Baslik = "Kitaplar", Controller = null, Action = null, AnaMenuId = null, Sirala = 6 },
                new MenuOgeModel { Id = 14, Baslik = "Kitap İşlemleri", Controller = "Kitaplar", Action = "Index", AnaMenuId = 13, Sirala = 1 },
                new MenuOgeModel { Id = 15, Baslik = "Kitap Hareketleri", Controller = "KitapDetaylar", Action = "Index", AnaMenuId = 13, Sirala = 2 },
                new MenuOgeModel { Id = 16, Baslik = "Cihazlar", Controller = null, Action = null, AnaMenuId = null, Sirala = 7 },
                new MenuOgeModel { Id = 17, Baslik = "Cihaz İşlemleri", Controller = "Cihazlar", Action = "Index", AnaMenuId = 16, Sirala = 1 },
                new MenuOgeModel { Id = 18, Baslik = "Raporlar", Controller = null, Action = null, AnaMenuId = null, Sirala = 8 },
                new MenuOgeModel { Id = 19, Baslik = "Öğrenci Giriş Çıkış Raporları", Controller = "OgrenciGirisCikis", Action = "Detay", AnaMenuId = 18, Sirala = 1 },
                new MenuOgeModel { Id = 20, Baslik = "Öğrenci Veli Raporu", Controller = "Ogrenciler", Action = "OgrenciVeliRapor", AnaMenuId = 18, Sirala = 2 },
                new MenuOgeModel { Id = 21, Baslik = "Öğrenci Aidat Raporu", Controller = "Aidat", Action = "AidatRapor", AnaMenuId = 18, Sirala = 3 },
                new MenuOgeModel { Id = 22, Baslik = "Öğrenci Ziyaretçi Raporu", Controller = "Ziyaretciler", Action = "ZiyaretciRapor", AnaMenuId = 18, Sirala = 4 },
                new MenuOgeModel { Id = 23, Baslik = "Öğrenci Yemek Raporu", Controller = "Yemekhane", Action = "YemekRapor", AnaMenuId = 18, Sirala = 5 },
                new MenuOgeModel { Id = 24, Baslik = "Kart Oku", Controller = null, Action = null, AnaMenuId = null, Sirala = 9 },
                new MenuOgeModel { Id = 25, Baslik = "Kart Okuma Ekranı", Controller = "KartOku", Action = "Index", AnaMenuId = 24, Sirala = 1 },
                new MenuOgeModel { Id = 26, Baslik = "Servisler", Controller = null, Action = null, AnaMenuId = null, Sirala = 10 },
                new MenuOgeModel { Id = 27, Baslik = "Servis İşlemleri", Controller = "Servisler", Action = "Index", AnaMenuId = 26, Sirala = 1 },
                new MenuOgeModel { Id = 28, Baslik = "Veliler", Controller = null, Action = null, AnaMenuId = null, Sirala = 11 },
                new MenuOgeModel { Id = 29, Baslik = "Veli İşlemleri", Controller = "Veliler", Action = "Index", AnaMenuId = 28, Sirala = 1 }
            );
        }
    }
}