using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class KullaniciAdiBenzersiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eski "Sil" akışı sadece öğretmen profilini pasifliyordu; bu öğretmenlerin hesabı da pasiflenir.
            migrationBuilder.Sql(@"
UPDATE K SET K.IsDeleted = 1
FROM Kullanicilar K
INNER JOIN OgretmenProfiller OP ON OP.KullaniciId = K.KullaniciId
WHERE K.Rol = 2 AND OP.IsDeleted = 1 AND K.IsDeleted = 0;");

            // Aktif kullanıcılar arasında mükerrer kullanıcı adı varsa index oluşturulamaz; anlaşılır hata ver.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Kullanicilar WHERE IsDeleted = 0 GROUP BY KullaniciAdi HAVING COUNT(*) > 1)
    THROW 50001, N'Aktif kullanıcılar arasında mükerrer KullaniciAdi var. Önce bu kayıtları yeniden adlandırın.', 1;");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar",
                column: "KullaniciAdi",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_KullaniciAdi",
                table: "Kullanicilar",
                column: "KullaniciAdi");
        }
    }
}
