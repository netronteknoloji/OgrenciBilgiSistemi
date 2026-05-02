using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class DuyurularEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Duyurular",
                columns: table => new
                {
                    DuyuruId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OlusturanKullaniciId = table.Column<int>(type: "int", nullable: false),
                    Hedef = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icerik = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Duyurular", x => x.DuyuruId);
                    table.ForeignKey(
                        name: "FK_Duyurular_Kullanicilar_OlusturanKullaniciId",
                        column: x => x.OlusturanKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Duyurular_Olusturan",
                table: "Duyurular",
                column: "OlusturanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Duyurular_Tarih",
                table: "Duyurular",
                column: "OlusturulmaTarihi");

            migrationBuilder.InsertData(
                table: "MenuOgeler",
                columns: new[] { "Id", "Action", "AnaMenuId", "Baslik", "Controller", "Sirala" },
                values: new object[,]
                {
                    { 33, null, null, "Duyurular", null, 13 },
                    { 34, "Index", 33, "Duyuru İşlemleri", "Duyurular", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "MenuOgeler", keyColumn: "Id", keyValue: 34);
            migrationBuilder.DeleteData(table: "MenuOgeler", keyColumn: "Id", keyValue: 33);
            migrationBuilder.DropTable(name: "Duyurular");
        }
    }
}
