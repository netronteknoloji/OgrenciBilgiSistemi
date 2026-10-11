using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class ServisSoforHostesBilgileri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HostesAdSoyad",
                table: "ServisProfiller",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostesTcKimlikNo",
                table: "ServisProfiller",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostesTelefon",
                table: "ServisProfiller",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoforAdSoyad",
                table: "ServisProfiller",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoforTcKimlikNo",
                table: "ServisProfiller",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostesAdSoyad",
                table: "ServisProfiller");

            migrationBuilder.DropColumn(
                name: "HostesTcKimlikNo",
                table: "ServisProfiller");

            migrationBuilder.DropColumn(
                name: "HostesTelefon",
                table: "ServisProfiller");

            migrationBuilder.DropColumn(
                name: "SoforAdSoyad",
                table: "ServisProfiller");

            migrationBuilder.DropColumn(
                name: "SoforTcKimlikNo",
                table: "ServisProfiller");
        }
    }
}
