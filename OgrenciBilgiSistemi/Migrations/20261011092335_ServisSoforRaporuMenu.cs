using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class ServisSoforRaporuMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuOgeler",
                columns: new[] { "Id", "Action", "AnaMenuId", "Baslik", "Controller", "GerekliRole", "Sirala" },
                values: new object[] { 35, "Index", 18, "Servis Şoför Raporu", "ServisRapor", null, 6 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuOgeler",
                keyColumn: "Id",
                keyValue: 35);
        }
    }
}
