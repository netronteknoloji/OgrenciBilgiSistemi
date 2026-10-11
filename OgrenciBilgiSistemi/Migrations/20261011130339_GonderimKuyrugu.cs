using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class GonderimKuyrugu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GonderimKuyrugu",
                columns: table => new
                {
                    GonderimKuyruguId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kanal = table.Column<byte>(type: "tinyint", nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TekilAnahtar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OgrenciId = table.Column<int>(type: "int", nullable: true),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Mesaj = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AliciKullaniciId = table.Column<int>(type: "int", nullable: true),
                    BildirimId = table.Column<int>(type: "int", nullable: true),
                    Durum = table.Column<byte>(type: "tinyint", nullable: false),
                    DenemeSayisi = table.Column<int>(type: "int", nullable: false),
                    SonrakiDeneme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KilitBitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GecerlilikBitis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GonderimZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonHata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GonderimKuyrugu", x => x.GonderimKuyruguId);
                    table.ForeignKey(
                        name: "FK_GonderimKuyrugu_Bildirimler_BildirimId",
                        column: x => x.BildirimId,
                        principalTable: "Bildirimler",
                        principalColumn: "BildirimId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GonderimKuyrugu_Durum_SonrakiDeneme",
                table: "GonderimKuyrugu",
                columns: new[] { "Durum", "SonrakiDeneme" });

            migrationBuilder.CreateIndex(
                name: "UX_GonderimKuyrugu_BildirimId",
                table: "GonderimKuyrugu",
                column: "BildirimId",
                unique: true,
                filter: "[BildirimId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_GonderimKuyrugu_TekilAnahtar",
                table: "GonderimKuyrugu",
                column: "TekilAnahtar",
                unique: true,
                filter: "[TekilAnahtar] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GonderimKuyrugu");
        }
    }
}
