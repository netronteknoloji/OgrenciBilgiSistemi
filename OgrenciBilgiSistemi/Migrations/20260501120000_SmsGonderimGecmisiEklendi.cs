using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class SmsGonderimGecmisiEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    DenemeNumarasi = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
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

            migrationBuilder.CreateIndex(
                name: "IX_SmsGonderimGecmisi_Ogrenci_Zaman",
                table: "SmsGonderimGecmisleri",
                columns: new[] { "OgrenciId", "GonderimZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsGonderimGecmisi_Tip_Zaman",
                table: "SmsGonderimGecmisleri",
                columns: new[] { "Tip", "GonderimZamani" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmsGonderimGecmisleri");
        }
    }
}
