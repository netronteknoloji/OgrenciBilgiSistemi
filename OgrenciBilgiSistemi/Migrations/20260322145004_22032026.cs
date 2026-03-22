using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class _22032026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Personeller_PersonelId",
                table: "Kullanicilar");

            migrationBuilder.DropForeignKey(
                name: "FK_Ogrenciler_Personeller_PersonelId",
                table: "Ogrenciler");

            migrationBuilder.DropForeignKey(
                name: "FK_SinifYoklamalar_Personeller_PersonelId",
                table: "SinifYoklamalar");

            migrationBuilder.DropForeignKey(
                name: "FK_Ziyaretciler_Personeller_PersonelId",
                table: "Ziyaretciler");

            migrationBuilder.DropTable(
                name: "PersonelDetaylar");

            migrationBuilder.DropTable(
                name: "Personeller");

            migrationBuilder.DropIndex(
                name: "UX_Kullanicilar_PersonelId",
                table: "Kullanicilar");

            migrationBuilder.RenameColumn(
                name: "PersonelId",
                table: "Ziyaretciler",
                newName: "KullaniciId");

            migrationBuilder.RenameIndex(
                name: "IX_Ziyaretciler_PersonelId",
                table: "Ziyaretciler",
                newName: "IX_Ziyaretciler_KullaniciId");

            migrationBuilder.RenameColumn(
                name: "PersonelId",
                table: "SinifYoklamalar",
                newName: "KullaniciId");

            migrationBuilder.RenameIndex(
                name: "IX_SinifYoklamalar_PersonelId",
                table: "SinifYoklamalar",
                newName: "IX_SinifYoklamalar_KullaniciId");

            migrationBuilder.RenameColumn(
                name: "PersonelId",
                table: "Ogrenciler",
                newName: "KullaniciId");

            migrationBuilder.RenameIndex(
                name: "IX_Ogrenciler_PersonelId",
                table: "Ogrenciler",
                newName: "IX_Ogrenciler_KullaniciId");


            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Kullanicilar",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GorselPath",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KartNo",
                table: "Kullanicilar",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_BirimId",
                table: "Kullanicilar",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "UX_Kullanicilar_KartNo",
                table: "Kullanicilar",
                column: "KartNo",
                unique: true,
                filter: "[KartNo] IS NOT NULL AND [KartNo] != ''");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Birimler_BirimId",
                table: "Kullanicilar",
                column: "BirimId",
                principalTable: "Birimler",
                principalColumn: "BirimId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Ogrenciler_Kullanicilar_KullaniciId",
                table: "Ogrenciler",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "KullaniciId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SinifYoklamalar_Kullanicilar_KullaniciId",
                table: "SinifYoklamalar",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "KullaniciId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ziyaretciler_Kullanicilar_KullaniciId",
                table: "Ziyaretciler",
                column: "KullaniciId",
                principalTable: "Kullanicilar",
                principalColumn: "KullaniciId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Birimler_BirimId",
                table: "Kullanicilar");

            migrationBuilder.DropForeignKey(
                name: "FK_Ogrenciler_Kullanicilar_KullaniciId",
                table: "Ogrenciler");

            migrationBuilder.DropForeignKey(
                name: "FK_SinifYoklamalar_Kullanicilar_KullaniciId",
                table: "SinifYoklamalar");

            migrationBuilder.DropForeignKey(
                name: "FK_Ziyaretciler_Kullanicilar_KullaniciId",
                table: "Ziyaretciler");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_BirimId",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "UX_Kullanicilar_KartNo",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "GorselPath",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "KartNo",
                table: "Kullanicilar");

            migrationBuilder.RenameColumn(
                name: "KullaniciId",
                table: "Ziyaretciler",
                newName: "PersonelId");

            migrationBuilder.RenameIndex(
                name: "IX_Ziyaretciler_KullaniciId",
                table: "Ziyaretciler",
                newName: "IX_Ziyaretciler_PersonelId");

            migrationBuilder.RenameColumn(
                name: "KullaniciId",
                table: "SinifYoklamalar",
                newName: "PersonelId");

            migrationBuilder.RenameIndex(
                name: "IX_SinifYoklamalar_KullaniciId",
                table: "SinifYoklamalar",
                newName: "IX_SinifYoklamalar_PersonelId");

            migrationBuilder.RenameColumn(
                name: "KullaniciId",
                table: "Ogrenciler",
                newName: "PersonelId");

            migrationBuilder.RenameIndex(
                name: "IX_Ogrenciler_KullaniciId",
                table: "Ogrenciler",
                newName: "IX_Ogrenciler_PersonelId");

            migrationBuilder.RenameColumn(
                name: "BirimId",
                table: "Kullanicilar",
                newName: "PersonelId");

            migrationBuilder.CreateTable(
                name: "Personeller",
                columns: table => new
                {
                    PersonelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BirimId = table.Column<int>(type: "int", nullable: true),
                    PersonelAdSoyad = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PersonelDurum = table.Column<bool>(type: "bit", nullable: false),
                    PersonelEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PersonelGorselPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonelKartNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PersonelTelefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personeller", x => x.PersonelId);
                    table.ForeignKey(
                        name: "FK_Personeller_Birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "Birimler",
                        principalColumn: "BirimId");
                });

            migrationBuilder.CreateTable(
                name: "PersonelDetaylar",
                columns: table => new
                {
                    PersonelDetayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CihazId = table.Column<int>(type: "int", nullable: true),
                    PersonelId = table.Column<int>(type: "int", nullable: false),
                    IstasyonTipi = table.Column<short>(type: "smallint", nullable: false),
                    PersonelCTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersonelGTarih = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PersonelGecisTipi = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    PersonelResimYolu = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    PersonelSmsGonderildi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelDetaylar", x => x.PersonelDetayId);
                    table.ForeignKey(
                        name: "FK_PersonelDetaylar_Cihazlar_CihazId",
                        column: x => x.CihazId,
                        principalTable: "Cihazlar",
                        principalColumn: "CihazId");
                    table.ForeignKey(
                        name: "FK_PersonelDetaylar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "PersonelId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Kullanicilar_PersonelId",
                table: "Kullanicilar",
                column: "PersonelId",
                unique: true,
                filter: "[PersonelId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelDetaylar_CihazId",
                table: "PersonelDetaylar",
                column: "CihazId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelDetaylar_PersonelId",
                table: "PersonelDetaylar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_BirimId",
                table: "Personeller",
                column: "BirimId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Personeller_PersonelId",
                table: "Kullanicilar",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "PersonelId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Ogrenciler_Personeller_PersonelId",
                table: "Ogrenciler",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "PersonelId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SinifYoklamalar_Personeller_PersonelId",
                table: "SinifYoklamalar",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "PersonelId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ziyaretciler_Personeller_PersonelId",
                table: "Ziyaretciler",
                column: "PersonelId",
                principalTable: "Personeller",
                principalColumn: "PersonelId");
        }
    }
}
