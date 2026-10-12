using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class CihazSenkronKuyrugu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CihazSenkronKuyrugu",
                columns: table => new
                {
                    CihazSenkronKuyruguId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CihazId = table.Column<int>(type: "int", nullable: false),
                    OgrenciId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<byte>(type: "tinyint", nullable: false),
                    SonIslem = table.Column<byte>(type: "tinyint", nullable: false),
                    DenemeSayisi = table.Column<int>(type: "int", nullable: false),
                    SonrakiDeneme = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KilitBitis = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TamamlanmaZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonHata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CihazSenkronKuyrugu", x => x.CihazSenkronKuyruguId);
                    table.ForeignKey(
                        name: "FK_CihazSenkronKuyrugu_Cihazlar_CihazId",
                        column: x => x.CihazId,
                        principalTable: "Cihazlar",
                        principalColumn: "CihazId");
                    table.ForeignKey(
                        name: "FK_CihazSenkronKuyrugu_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "OgrenciId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CihazSenkronKuyrugu_Cihaz_Durum",
                table: "CihazSenkronKuyrugu",
                columns: new[] { "CihazId", "Durum" });

            migrationBuilder.CreateIndex(
                name: "IX_CihazSenkronKuyrugu_Durum_SonrakiDeneme",
                table: "CihazSenkronKuyrugu",
                columns: new[] { "Durum", "SonrakiDeneme" });

            migrationBuilder.CreateIndex(
                name: "IX_CihazSenkronKuyrugu_OgrenciId",
                table: "CihazSenkronKuyrugu",
                column: "OgrenciId");

            migrationBuilder.CreateIndex(
                name: "UX_CihazSenkronKuyrugu_Bekleyen",
                table: "CihazSenkronKuyrugu",
                columns: new[] { "CihazId", "OgrenciId" },
                unique: true,
                filter: "[Durum] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CihazSenkronKuyrugu");
        }
    }
}
