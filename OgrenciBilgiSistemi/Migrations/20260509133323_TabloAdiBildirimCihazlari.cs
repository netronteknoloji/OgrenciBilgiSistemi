using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class TabloAdiBildirimCihazlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushCihazlari");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BildirimCihazlari");

            migrationBuilder.CreateTable(
                name: "PushCihazlari",
                columns: table => new
                {
                    PushCihaziId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    CihazModeli = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FcmToken = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Platform = table.Column<byte>(type: "tinyint", nullable: false),
                    SonGuncelleme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UygulamaSurumu = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushCihazlari", x => x.PushCihaziId);
                    table.ForeignKey(
                        name: "FK_PushCihazlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushCihazlari_Kullanici_Aktif",
                table: "PushCihazlari",
                columns: new[] { "KullaniciId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UX_PushCihazlari_FcmToken_Aktif",
                table: "PushCihazlari",
                column: "FcmToken",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
