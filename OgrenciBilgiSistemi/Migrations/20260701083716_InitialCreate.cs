using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Birimler",
                columns: table => new
                {
                    BirimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimAd = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BirimSinifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Birimler", x => x.BirimId);
                });

            migrationBuilder.CreateTable(
                name: "Cihazlar",
                columns: table => new
                {
                    CihazId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CihazAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CihazKodu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonanimTipi = table.Column<byte>(type: "tinyint", nullable: false),
                    IstasyonTipi = table.Column<short>(type: "smallint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IpAdresi = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    PortNo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cihazlar", x => x.CihazId);
                });

            migrationBuilder.CreateTable(
                name: "Kitaplar",
                columns: table => new
                {
                    KitapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitapAd = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KitapGorsel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KitapTurAd = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    KitapGun = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kitaplar", x => x.KitapId);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Sifre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeniHatirla = table.Column<bool>(type: "bit", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.KullaniciId);
                });

            migrationBuilder.CreateTable(
                name: "MenuOgeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Baslik = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Controller = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GerekliRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sirala = table.Column<int>(type: "int", nullable: false),
                    AnaMenuId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuOgeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuOgeler_MenuOgeler_AnaMenuId",
                        column: x => x.AnaMenuId,
                        principalTable: "MenuOgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciAidatTarifeler",
                columns: table => new
                {
                    OgrenciAidatTarifeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaslangicYil = table.Column<int>(type: "int", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciAidatTarifeler", x => x.OgrenciAidatTarifeId);
                    table.CheckConstraint("CK_Tarife_BaslangicYil", "[BaslangicYil] BETWEEN 2000 AND 2100");
                    table.CheckConstraint("CK_Tarife_Tutar", "[Tutar] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "BildirimCihazlari",
                columns: table => new
                {
                    BildirimCihaziId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    FcmToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<byte>(type: "tinyint", nullable: false),
                    UygulamaSurumu = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CihazModeli = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SonGuncelleme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BildirimCihazlari", x => x.BildirimCihaziId);
                    table.ForeignKey(
                        name: "FK_BildirimCihazlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Duyurular",
                columns: table => new
                {
                    DuyuruId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    Hedef = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Duyurular", x => x.DuyuruId);
                    table.ForeignKey(
                        name: "FK_Duyurular_Kullanicilar_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ogrenciler",
                columns: table => new
                {
                    OgrenciId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciAdSoyad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OgrenciNo = table.Column<int>(type: "int", nullable: false),
                    OgrenciKartNo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VeliId = table.Column<int>(type: "int", nullable: true),
                    OgrenciCikisDurumu = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OgretmenId = table.Column<int>(type: "int", nullable: true),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    ServisId = table.Column<int>(type: "int", nullable: true),
                    OgrenciGorsel = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ogrenciler", x => x.OgrenciId);
                    table.ForeignKey(
                        name: "FK_Ogrenciler_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "BirimId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ogrenciler_Kullanicilar_OgretmenId",
                        column: x => x.OgretmenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ogrenciler_Kullanicilar_ServisId",
                        column: x => x.ServisId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ogrenciler_Kullanicilar_VeliId",
                        column: x => x.VeliId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgretmenProfiller",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    GorselPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgretmenProfiller", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_OgretmenProfiller_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "BirimId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OgretmenProfiller_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OgretmenRandevular",
                columns: table => new
                {
                    OgretmenRandevuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgretmenKullaniciId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaslangicSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    BitisSaati = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgretmenRandevular", x => x.OgretmenRandevuId);
                    table.ForeignKey(
                        name: "FK_OgretmenRandevular_Kullanicilar_OgretmenKullaniciId",
                        column: x => x.OgretmenKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServisProfiller",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Plaka = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisProfiller", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_ServisProfiller_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VeliProfiller",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    VeliAdres = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VeliMeslek = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VeliIsYeri = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VeliEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VeliYakinlik = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeliProfiller", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_VeliProfiller_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ziyaretciler",
                columns: table => new
                {
                    ZiyaretciId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdSoyad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TcKimlikNo = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Adres = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    KullaniciId = table.Column<int>(type: "int", nullable: true),
                    ZiyaretSebebi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KartNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KartVerildiMi = table.Column<bool>(type: "bit", nullable: false),
                    GirisZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CikisZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    CihazId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ziyaretciler", x => x.ZiyaretciId);
                    table.ForeignKey(
                        name: "FK_Ziyaretciler_Cihazlar_CihazId",
                        column: x => x.CihazId,
                        principalTable: "Cihazlar",
                        principalColumn: "CihazId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ziyaretciler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciMenuOgeler",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    MenuOgeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciMenuOgeler", x => new { x.KullaniciId, x.MenuOgeId });
                    table.ForeignKey(
                        name: "FK_KullaniciMenuOgeler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KullaniciMenuOgeler_MenuOgeler_MenuOgeId",
                        column: x => x.MenuOgeId,
                        principalTable: "MenuOgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DuyuruOkumalari",
                columns: table => new
                {
                    DuyuruOkumaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DuyuruId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    OkunduTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuyuruOkumalari", x => x.DuyuruOkumaId);
                    table.ForeignKey(
                        name: "FK_DuyuruOkumalari_Duyurular_DuyuruId",
                        column: x => x.DuyuruId,
                        principalTable: "Duyurular",
                        principalColumn: "DuyuruId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuyuruOkumalari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KitapDetaylar",
                columns: table => new
                {
                    KitapDetayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KitapAlTarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KitapVerTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KitapDurum = table.Column<int>(type: "int", nullable: false),
                    KitapId = table.Column<int>(type: "int", nullable: false),
                    OgrenciId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapDetaylar", x => x.KitapDetayId);
                    table.ForeignKey(
                        name: "FK_KitapDetaylar_Kitaplar_KitapId",
                        column: x => x.KitapId,
                        principalTable: "Kitaplar",
                        principalColumn: "KitapId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KitapDetaylar_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciAidatlar",
                columns: table => new
                {
                    OgrenciAidatId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    BaslangicYil = table.Column<int>(type: "int", nullable: false),
                    Borc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Odenen = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Muaf = table.Column<bool>(type: "bit", nullable: false),
                    SonOdemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciAidatlar", x => x.OgrenciAidatId);
                    table.CheckConstraint("CK_Aidat_BaslangicYil", "[BaslangicYil] BETWEEN 2000 AND 2100");
                    table.CheckConstraint("CK_Aidat_Pozitif", "[Borc] >= 0 AND [Odenen] >= 0");
                    table.ForeignKey(
                        name: "FK_OgrenciAidatlar_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciDetaylar",
                columns: table => new
                {
                    OgrenciDetayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    IstasyonTipi = table.Column<short>(type: "smallint", nullable: false),
                    OgrenciGTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OgrenciCTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OgrenciGecisTipi = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    OgrenciSmsGonderildi = table.Column<bool>(type: "bit", nullable: true),
                    OgrenciResimYolu = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    CihazId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciDetaylar", x => x.OgrenciDetayId);
                    table.ForeignKey(
                        name: "FK_OgrenciDetaylar_Cihazlar_CihazId",
                        column: x => x.CihazId,
                        principalTable: "Cihazlar",
                        principalColumn: "CihazId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OgrenciDetaylar_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciYemekler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false),
                    Not = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciYemekler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OgrenciYemekler_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciYemekOdemeler",
                columns: table => new
                {
                    OgrenciYemekOdemeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    Ay = table.Column<int>(type: "int", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciYemekOdemeler", x => x.OgrenciYemekOdemeId);
                    table.ForeignKey(
                        name: "FK_OgrenciYemekOdemeler_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciYemekTarifeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    Yil = table.Column<int>(type: "int", nullable: false),
                    AylikTutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciYemekTarifeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OgrenciYemekTarifeler_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Randevular",
                columns: table => new
                {
                    RandevuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgretmenKullaniciId = table.Column<int>(type: "int", nullable: false),
                    VeliKullaniciId = table.Column<int>(type: "int", nullable: false),
                    OgrenciId = table.Column<int>(type: "int", nullable: true),
                    RandevuTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SureDakika = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Not = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OgretmenTarafindanOlusturuldu = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Randevular", x => x.RandevuId);
                    table.ForeignKey(
                        name: "FK_Randevular_Kullanicilar_OgretmenKullaniciId",
                        column: x => x.OgretmenKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Randevular_Kullanicilar_VeliKullaniciId",
                        column: x => x.VeliKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Randevular_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServisYoklamalar",
                columns: table => new
                {
                    ServisYoklamaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    DurumId = table.Column<int>(type: "int", nullable: false),
                    Periyot = table.Column<int>(type: "int", nullable: false),
                    SmsGonderildi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServisYoklamalar", x => x.ServisYoklamaId);
                    table.ForeignKey(
                        name: "FK_ServisYoklamalar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServisYoklamalar_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SinifYoklamalar",
                columns: table => new
                {
                    SinifYoklamaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Ders1 = table.Column<int>(type: "int", nullable: true),
                    Ders2 = table.Column<int>(type: "int", nullable: true),
                    Ders3 = table.Column<int>(type: "int", nullable: true),
                    Ders4 = table.Column<int>(type: "int", nullable: true),
                    Ders5 = table.Column<int>(type: "int", nullable: true),
                    Ders6 = table.Column<int>(type: "int", nullable: true),
                    Ders7 = table.Column<int>(type: "int", nullable: true),
                    Ders8 = table.Column<int>(type: "int", nullable: true),
                    SmsDurumu = table.Column<int>(type: "int", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinifYoklamalar", x => x.SinifYoklamaId);
                    table.ForeignKey(
                        name: "FK_SinifYoklamalar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinifYoklamalar_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SmsGonderimGecmisleri",
                columns: table => new
                {
                    SmsGonderimGecmisiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciId = table.Column<int>(type: "int", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GonderimZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Basarili = table.Column<bool>(type: "bit", nullable: false),
                    HataKategorisi = table.Column<int>(type: "int", nullable: false),
                    Hata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HamCevap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HttpDurumKodu = table.Column<int>(type: "int", nullable: true),
                    DenemeNumarasi = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsGonderimGecmisleri", x => x.SmsGonderimGecmisiId);
                    table.ForeignKey(
                        name: "FK_SmsGonderimGecmisleri_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciAidatOdemeler",
                columns: table => new
                {
                    OgrenciAidatOdemeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OgrenciAidatId = table.Column<int>(type: "int", nullable: false),
                    OdemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tutar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OdemeTipi = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciAidatOdemeler", x => x.OgrenciAidatOdemeId);
                    table.CheckConstraint("CK_AidatOdeme_Tutar_NonNegative", "[Tutar] >= 0");
                    table.ForeignKey(
                        name: "FK_OgrenciAidatOdemeler_OgrenciAidatlar_OgrenciAidatId",
                        column: x => x.OgrenciAidatId,
                        principalTable: "OgrenciAidatlar",
                        principalColumn: "OgrenciAidatId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    BildirimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AliciKullaniciId = table.Column<int>(type: "int", nullable: false),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RandevuId = table.Column<int>(type: "int", nullable: true),
                    Okundu = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.BildirimId);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Kullanicilar_AliciKullaniciId",
                        column: x => x.AliciKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Randevular_RandevuId",
                        column: x => x.RandevuId,
                        principalTable: "Randevular",
                        principalColumn: "RandevuId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MenuOgeler",
                columns: new[] { "Id", "Action", "AnaMenuId", "Baslik", "Controller", "GerekliRole", "Sirala" },
                values: new object[,]
                {
                    { 1, "Index", null, "Ana Sayfa", "Home", null, 1 },
                    { 2, null, null, "Öğretmenler", null, null, 2 },
                    { 5, null, null, "Öğrenciler", null, null, 3 },
                    { 9, null, null, "Ziyaretçiler", null, null, 4 },
                    { 11, null, null, "Kullanıcılar", null, null, 5 },
                    { 13, null, null, "Kitaplar", null, null, 6 },
                    { 16, null, null, "Cihazlar", null, null, 7 },
                    { 18, null, null, "Raporlar", null, null, 8 },
                    { 24, null, null, "Kart Oku", null, null, 9 },
                    { 26, null, null, "Servisler", null, null, 10 },
                    { 28, null, null, "Veliler", null, null, 11 },
                    { 30, null, null, "Randevular", null, null, 12 },
                    { 33, null, null, "Duyurular", null, null, 13 },
                    { 3, "Index", 2, "Birim İşlemleri", "Birimler", null, 1 },
                    { 4, "Index", 2, "Öğretmen İşlemleri", "Ogretmenler", null, 2 },
                    { 6, "Index", 5, "Öğrenci İşlemleri", "Ogrenciler", null, 1 },
                    { 7, "Index", 5, "Aidat İşlemleri", "Aidat", null, 2 },
                    { 8, "Index", 5, "Yemekhane İşlemleri", "Yemekhane", null, 3 },
                    { 10, "Index", 9, "Ziyaretçi İşlemleri", "Ziyaretciler", null, 1 },
                    { 12, "Index", 11, "Kullanıcı İşlemleri", "Kullanicilar", null, 1 },
                    { 14, "Index", 13, "Kitap İşlemleri", "Kitaplar", null, 1 },
                    { 15, "Index", 13, "Kitap Hareketleri", "KitapDetaylar", null, 2 },
                    { 17, "Index", 16, "Cihaz İşlemleri", "Cihazlar", null, 1 },
                    { 19, "Detay", 18, "Öğrenci Giriş Çıkış Raporları", "OgrenciGirisCikis", null, 1 },
                    { 20, "OgrenciVeliRapor", 18, "Öğrenci Veli Raporu", "Ogrenciler", null, 2 },
                    { 21, "AidatRapor", 18, "Öğrenci Aidat Raporu", "Aidat", null, 3 },
                    { 22, "ZiyaretciRapor", 18, "Öğrenci Ziyaretçi Raporu", "Ziyaretciler", null, 4 },
                    { 23, "YemekRapor", 18, "Öğrenci Yemek Raporu", "Yemekhane", null, 5 },
                    { 25, "Index", 24, "Kart Okuma Ekranı", "KartOku", null, 1 },
                    { 27, "Index", 26, "Servis İşlemleri", "Servisler", null, 1 },
                    { 29, "Index", 28, "Veli İşlemleri", "Veliler", null, 1 },
                    { 31, "Index", 30, "Randevu Listesi", "Randevular", null, 1 },
                    { 32, "Index", 30, "Öğretmen Randevu Takvimi", "OgretmenRandevu", null, 2 },
                    { 34, "Index", 33, "Duyuru İşlemleri", "Duyurular", null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BildirimCihazlari_Kullanici_Aktif",
                table: "BildirimCihazlari",
                columns: new[] { "KullaniciId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UX_BildirimCihazlari_FcmToken_Aktif",
                table: "BildirimCihazlari",
                column: "FcmToken",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_Alici_Okundu",
                table: "Bildirimler",
                columns: new[] { "AliciKullaniciId", "Okundu" });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_RandevuId",
                table: "Bildirimler",
                column: "RandevuId");

            migrationBuilder.CreateIndex(
                name: "IX_Cihazlar_CihazAdi",
                table: "Cihazlar",
                column: "CihazAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cihazlar_CihazKodu",
                table: "Cihazlar",
                column: "CihazKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cihazlar_IstasyonTipi",
                table: "Cihazlar",
                column: "IstasyonTipi");

            migrationBuilder.CreateIndex(
                name: "IX_Duyurular_Olusturan",
                table: "Duyurular",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Duyurular_Tarih",
                table: "Duyurular",
                column: "OlusturulmaTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_DuyuruOkumalar_Duyuru_Kullanici_Unique",
                table: "DuyuruOkumalari",
                columns: new[] { "DuyuruId", "KullaniciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuyuruOkumalar_Kullanici",
                table: "DuyuruOkumalari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_KitapDetaylar_KitapId",
                table: "KitapDetaylar",
                column: "KitapId");

            migrationBuilder.CreateIndex(
                name: "IX_KitapDetaylar_OgrenciId",
                table: "KitapDetaylar",
                column: "OgrenciId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar",
                column: "KullaniciAdi");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Rol",
                table: "Kullanicilar",
                column: "Rol");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Telefon",
                table: "Kullanicilar",
                column: "Telefon",
                filter: "[Telefon] IS NOT NULL AND [Telefon] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciMenuOgeler_MenuOgeId",
                table: "KullaniciMenuOgeler",
                column: "MenuOgeId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuOgeler_AnaMenuId_Sirala",
                table: "MenuOgeler",
                columns: new[] { "AnaMenuId", "Sirala" });

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciAidatlar_OgrenciId_BaslangicYil",
                table: "OgrenciAidatlar",
                columns: new[] { "OgrenciId", "BaslangicYil" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciAidatOdemeler_OgrenciAidatId_OdemeTarihi",
                table: "OgrenciAidatOdemeler",
                columns: new[] { "OgrenciAidatId", "OdemeTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciAidatTarifeler_BaslangicYil",
                table: "OgrenciAidatTarifeler",
                column: "BaslangicYil",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDetaylar_CihazId",
                table: "OgrenciDetaylar",
                column: "CihazId");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDetaylar_OgrenciCTarih",
                table: "OgrenciDetaylar",
                column: "OgrenciCTarih");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDetaylar_OgrenciGTarih",
                table: "OgrenciDetaylar",
                column: "OgrenciGTarih");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDetaylar_OgrenciId_IstasyonTipi",
                table: "OgrenciDetaylar",
                columns: new[] { "OgrenciId", "IstasyonTipi" });

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_BirimId",
                table: "Ogrenciler",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_OgrenciNo",
                table: "Ogrenciler",
                column: "OgrenciNo");

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_OgretmenId",
                table: "Ogrenciler",
                column: "OgretmenId");

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_ServisId",
                table: "Ogrenciler",
                column: "ServisId");

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_VeliId",
                table: "Ogrenciler",
                column: "VeliId");

            migrationBuilder.CreateIndex(
                name: "UX_Ogrenciler_OgrenciKartNo",
                table: "Ogrenciler",
                column: "OgrenciKartNo",
                unique: true,
                filter: "[OgrenciKartNo] IS NOT NULL AND [OgrenciKartNo] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciYemekler_OgrenciId_Yil_Ay",
                table: "OgrenciYemekler",
                columns: new[] { "OgrenciId", "Yil", "Ay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciYemekOdemeler_OgrenciId_Yil_Ay",
                table: "OgrenciYemekOdemeler",
                columns: new[] { "OgrenciId", "Yil", "Ay" });

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciYemekTarifeler_OgrenciId_Yil",
                table: "OgrenciYemekTarifeler",
                columns: new[] { "OgrenciId", "Yil" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgretmenProfiller_BirimId",
                table: "OgretmenProfiller",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "UX_OgretmenRandevular_Slot",
                table: "OgretmenRandevular",
                columns: new[] { "OgretmenKullaniciId", "Tarih", "BaslangicSaati" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_OgrenciId",
                table: "Randevular",
                column: "OgrenciId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_OgretmenTarih",
                table: "Randevular",
                columns: new[] { "OgretmenKullaniciId", "RandevuTarihi" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_Tarih",
                table: "Randevular",
                column: "RandevuTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_VeliKullaniciId",
                table: "Randevular",
                column: "VeliKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_ServisYoklamalar_KullaniciId_OgrenciId_Periyot_OlusturulmaTarihi",
                table: "ServisYoklamalar",
                columns: new[] { "KullaniciId", "OgrenciId", "Periyot", "OlusturulmaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_ServisYoklamalar_OgrenciId",
                table: "ServisYoklamalar",
                column: "OgrenciId");

            migrationBuilder.CreateIndex(
                name: "IX_SinifYoklamalar_KullaniciId",
                table: "SinifYoklamalar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SinifYoklamalar_OgrenciId_OlusturulmaTarihi",
                table: "SinifYoklamalar",
                columns: new[] { "OgrenciId", "OlusturulmaTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsGonderimGecmisi_Ogrenci_Zaman",
                table: "SmsGonderimGecmisleri",
                columns: new[] { "OgrenciId", "GonderimZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsGonderimGecmisi_Tip_Zaman",
                table: "SmsGonderimGecmisleri",
                columns: new[] { "Tip", "GonderimZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_Ziyaretciler_CihazId",
                table: "Ziyaretciler",
                column: "CihazId");

            migrationBuilder.CreateIndex(
                name: "IX_Ziyaretciler_KullaniciId",
                table: "Ziyaretciler",
                column: "KullaniciId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BildirimCihazlari");

            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "DuyuruOkumalari");

            migrationBuilder.DropTable(
                name: "KitapDetaylar");

            migrationBuilder.DropTable(
                name: "KullaniciMenuOgeler");

            migrationBuilder.DropTable(
                name: "OgrenciAidatOdemeler");

            migrationBuilder.DropTable(
                name: "OgrenciAidatTarifeler");

            migrationBuilder.DropTable(
                name: "OgrenciDetaylar");

            migrationBuilder.DropTable(
                name: "OgrenciYemekler");

            migrationBuilder.DropTable(
                name: "OgrenciYemekOdemeler");

            migrationBuilder.DropTable(
                name: "OgrenciYemekTarifeler");

            migrationBuilder.DropTable(
                name: "OgretmenProfiller");

            migrationBuilder.DropTable(
                name: "OgretmenRandevular");

            migrationBuilder.DropTable(
                name: "ServisProfiller");

            migrationBuilder.DropTable(
                name: "ServisYoklamalar");

            migrationBuilder.DropTable(
                name: "SinifYoklamalar");

            migrationBuilder.DropTable(
                name: "SmsGonderimGecmisleri");

            migrationBuilder.DropTable(
                name: "VeliProfiller");

            migrationBuilder.DropTable(
                name: "Ziyaretciler");

            migrationBuilder.DropTable(
                name: "Randevular");

            migrationBuilder.DropTable(
                name: "Duyurular");

            migrationBuilder.DropTable(
                name: "Kitaplar");

            migrationBuilder.DropTable(
                name: "MenuOgeler");

            migrationBuilder.DropTable(
                name: "OgrenciAidatlar");

            migrationBuilder.DropTable(
                name: "Cihazlar");

            migrationBuilder.DropTable(
                name: "Ogrenciler");

            migrationBuilder.DropTable(
                name: "Birimler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
