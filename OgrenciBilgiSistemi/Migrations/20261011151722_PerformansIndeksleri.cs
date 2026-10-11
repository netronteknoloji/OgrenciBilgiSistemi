using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class PerformansIndeksleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OlayZamani",
                table: "OgrenciDetaylar",
                type: "datetime2",
                nullable: true,
                computedColumnSql: "COALESCE([OgrenciGTarih],[OgrenciCTarih])",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_SinifYoklamalar_OlusturulmaTarihi",
                table: "SinifYoklamalar",
                column: "OlusturulmaTarihi")
                .Annotation("SqlServer:Include", new[] { "OgrenciId", "SmsDurumu", "Ders1", "Ders2", "Ders3", "Ders4", "Ders5", "Ders6", "Ders7", "Ders8" });

            migrationBuilder.CreateIndex(
                name: "IX_ServisYoklamalar_OlusturulmaTarihi",
                table: "ServisYoklamalar",
                column: "OlusturulmaTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDetaylar_OgrenciId_OlayZamani",
                table: "OgrenciDetaylar",
                columns: new[] { "OgrenciId", "OlayZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciDetaylar_OlayZamani",
                table: "OgrenciDetaylar",
                column: "OlayZamani")
                .Annotation("SqlServer:Include", new[] { "OgrenciId", "IstasyonTipi", "OgrenciSmsGonderildi", "OgrenciGTarih", "OgrenciCTarih" });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_Alici_Tarih",
                table: "Bildirimler",
                columns: new[] { "AliciKullaniciId", "OlusturulmaTarihi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SinifYoklamalar_OlusturulmaTarihi",
                table: "SinifYoklamalar");

            migrationBuilder.DropIndex(
                name: "IX_ServisYoklamalar_OlusturulmaTarihi",
                table: "ServisYoklamalar");

            migrationBuilder.DropIndex(
                name: "IX_OgrenciDetaylar_OgrenciId_OlayZamani",
                table: "OgrenciDetaylar");

            migrationBuilder.DropIndex(
                name: "IX_OgrenciDetaylar_OlayZamani",
                table: "OgrenciDetaylar");

            migrationBuilder.DropIndex(
                name: "IX_Bildirimler_Alici_Tarih",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "OlayZamani",
                table: "OgrenciDetaylar");
        }
    }
}
