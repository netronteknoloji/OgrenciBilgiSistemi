using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class _290320261 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SinifYoklamaDurumlar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SinifYoklamaDurumlar",
                columns: table => new
                {
                    DurumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DurumAd = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinifYoklamaDurumlar", x => x.DurumId);
                });

            migrationBuilder.InsertData(
                table: "SinifYoklamaDurumlar",
                columns: new[] { "DurumId", "DurumAd" },
                values: new object[,]
                {
                    { 1, "Var" },
                    { 2, "Yok" },
                    { 3, "Geç" },
                    { 4, "İzinli" },
                    { 5, "Raporlu" }
                });
        }
    }
}
