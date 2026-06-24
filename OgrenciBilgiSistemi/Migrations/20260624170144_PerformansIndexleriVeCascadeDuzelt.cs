using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class PerformansIndexleriVeCascadeDuzelt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KullaniciMenuOgeler_MenuOgeler_MenuOgeId",
                table: "KullaniciMenuOgeler");

            migrationBuilder.DropIndex(
                name: "IX_Randevular_OgretmenKullaniciId",
                table: "Randevular");

            migrationBuilder.DropIndex(
                name: "IX_OgretmenRandevular_OgretmenKullaniciId",
                table: "OgretmenRandevular");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_OgretmenTarih",
                table: "Randevular",
                columns: new[] { "OgretmenKullaniciId", "RandevuTarihi" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OgretmenRandevular_Slot",
                table: "OgretmenRandevular",
                columns: new[] { "OgretmenKullaniciId", "Tarih", "BaslangicSaati" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Telefon",
                table: "Kullanicilar",
                column: "Telefon",
                filter: "[Telefon] IS NOT NULL AND [Telefon] != ''");

            migrationBuilder.AddForeignKey(
                name: "FK_KullaniciMenuOgeler_MenuOgeler_MenuOgeId",
                table: "KullaniciMenuOgeler",
                column: "MenuOgeId",
                principalTable: "MenuOgeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KullaniciMenuOgeler_MenuOgeler_MenuOgeId",
                table: "KullaniciMenuOgeler");

            migrationBuilder.DropIndex(
                name: "IX_Randevular_OgretmenTarih",
                table: "Randevular");

            migrationBuilder.DropIndex(
                name: "UX_OgretmenRandevular_Slot",
                table: "OgretmenRandevular");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_Telefon",
                table: "Kullanicilar");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_OgretmenKullaniciId",
                table: "Randevular",
                column: "OgretmenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_OgretmenRandevular_OgretmenKullaniciId",
                table: "OgretmenRandevular",
                column: "OgretmenKullaniciId");

            migrationBuilder.AddForeignKey(
                name: "FK_KullaniciMenuOgeler_MenuOgeler_MenuOgeId",
                table: "KullaniciMenuOgeler",
                column: "MenuOgeId",
                principalTable: "MenuOgeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
