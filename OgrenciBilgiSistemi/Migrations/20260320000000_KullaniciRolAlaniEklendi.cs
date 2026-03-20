using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class KullaniciRolAlaniEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Yeni Rol kolonunu ekle (varsayılan: 2 = Ogretmen)
            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "Kullanicilar",
                type: "int",
                nullable: false,
                defaultValue: 2);

            // 2) Mevcut verileri dönüştür: AdminMi = true → Rol = 1 (Admin)
            migrationBuilder.Sql(
                "UPDATE Kullanicilar SET Rol = 1 WHERE AdminMi = 1");

            // 3) Servise atanmış kullanıcıları Şoför yap: Rol = 3
            migrationBuilder.Sql(@"
                UPDATE K SET K.Rol = 3
                FROM Kullanicilar K
                INNER JOIN Servisler S ON S.KullaniciId = K.KullaniciId");

            // 4) AdminMi kolonunu kaldır
            migrationBuilder.DropColumn(
                name: "AdminMi",
                table: "Kullanicilar");

            // 5) Menüye Servisler öğelerini ekle
            migrationBuilder.InsertData(
                table: "MenuOgeler",
                columns: new[] { "Id", "Baslik", "Controller", "Action", "AnaMenuId", "Sirala" },
                values: new object[] { 26, "Servisler", null, null, null, 10 });

            migrationBuilder.InsertData(
                table: "MenuOgeler",
                columns: new[] { "Id", "Baslik", "Controller", "Action", "AnaMenuId", "Sirala" },
                values: new object[] { 27, "Servis Listesi", "Servisler", "Index", 26, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1) AdminMi kolonunu geri ekle
            migrationBuilder.AddColumn<bool>(
                name: "AdminMi",
                table: "Kullanicilar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 2) Rol = 1 (Admin) → AdminMi = true
            migrationBuilder.Sql(
                "UPDATE Kullanicilar SET AdminMi = 1 WHERE Rol = 1");

            // 3) Rol kolonunu kaldır
            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Kullanicilar");

            // 4) Menü öğelerini kaldır
            migrationBuilder.DeleteData(table: "MenuOgeler", keyColumn: "Id", keyValue: 27);
            migrationBuilder.DeleteData(table: "MenuOgeler", keyColumn: "Id", keyValue: 26);
        }
    }
}
