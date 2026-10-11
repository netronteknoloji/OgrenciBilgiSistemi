using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OgrenciBilgiSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AidatOdenenKaldirVeTutarKurali : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AidatOdeme_Tutar_NonNegative",
                table: "OgrenciAidatOdemeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Aidat_Pozitif",
                table: "OgrenciAidatlar");

            migrationBuilder.DropColumn(
                name: "Odenen",
                table: "OgrenciAidatlar");

            migrationBuilder.DropColumn(
                name: "SonOdemeTarihi",
                table: "OgrenciAidatlar");

            migrationBuilder.AlterColumn<decimal>(
                name: "Borc",
                table: "OgrenciAidatlar",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            // Tarifeye eşit veya 0 olan borçlar "tarifeyi kullan" (NULL) durumuna çevrilir;
            // eski kod Borc <= 0 kaydı zaten tarife olarak yorumluyordu. Farklı tutarlar özel borç olarak kalır.
            migrationBuilder.Sql(@"
UPDATE a SET a.Borc = NULL
FROM OgrenciAidatlar a
LEFT JOIN OgrenciAidatTarifeler t ON t.BaslangicYil = a.BaslangicYil
WHERE a.Borc <= 0 OR a.Borc = t.Tutar;");

            // 0 TL ödemeler yeni kurala takılmasın diye silindi olarak işaretlenir.
            migrationBuilder.Sql("UPDATE OgrenciAidatOdemeler SET IsDeleted = 1 WHERE Tutar = 0;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AidatOdeme_Tutar_Pozitif",
                table: "OgrenciAidatOdemeler",
                sql: "[Tutar] > 0 OR [IsDeleted] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Aidat_Pozitif",
                table: "OgrenciAidatlar",
                sql: "[Borc] IS NULL OR [Borc] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AidatOdeme_Tutar_Pozitif",
                table: "OgrenciAidatOdemeler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Aidat_Pozitif",
                table: "OgrenciAidatlar");

            // NULL borçlar (tarifeyi kullanan) yılın tarifesiyle doldurulur.
            migrationBuilder.Sql(@"
UPDATE a SET a.Borc = ISNULL(t.Tutar, 0)
FROM OgrenciAidatlar a
LEFT JOIN OgrenciAidatTarifeler t ON t.BaslangicYil = a.BaslangicYil
WHERE a.Borc IS NULL;");

            migrationBuilder.AlterColumn<decimal>(
                name: "Borc",
                table: "OgrenciAidatlar",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Odenen",
                table: "OgrenciAidatlar",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SonOdemeTarihi",
                table: "OgrenciAidatlar",
                type: "datetime2",
                nullable: true);

            // Kaldırılan kolonlar ödeme satırlarından yeniden hesaplanır.
            migrationBuilder.Sql(@"
UPDATE a SET
    a.Odenen = ISNULL(o.Toplam, 0),
    a.SonOdemeTarihi = o.SonTarih
FROM OgrenciAidatlar a
OUTER APPLY (
    SELECT SUM(p.Tutar) AS Toplam, MAX(p.OdemeTarihi) AS SonTarih
    FROM OgrenciAidatOdemeler p
    WHERE p.OgrenciAidatId = a.OgrenciAidatId AND p.IsDeleted = 0
) o;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AidatOdeme_Tutar_NonNegative",
                table: "OgrenciAidatOdemeler",
                sql: "[Tutar] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Aidat_Pozitif",
                table: "OgrenciAidatlar",
                sql: "[Borc] >= 0 AND [Odenen] >= 0");
        }
    }
}
