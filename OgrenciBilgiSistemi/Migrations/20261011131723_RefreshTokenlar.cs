using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenlar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokenlar",
                columns: table => new
                {
                    RefreshTokenId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    OturumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SonKullanim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IptalZamani = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokenlar", x => x.RefreshTokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenlar_KullaniciId",
                table: "RefreshTokenlar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokenlar_OturumId",
                table: "RefreshTokenlar",
                column: "OturumId");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokenlar_TokenHash",
                table: "RefreshTokenlar",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokenlar");
        }
    }
}
