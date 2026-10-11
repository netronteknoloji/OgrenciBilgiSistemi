-- PerformansIndeksleri migration'ının elle uygulanabilir karşılığı (sorgu indeksleri).
-- Tekrar çalıştırılabilir; her okul DB'sinde, yedekten sonra çalıştırın:
--   sqlcmd -S <sunucu> -d <okulDb> -E -C -f 65001 -i performans-indeksleri.sql
-- Computed column indeksi için zorunlu SET seçenekleri:
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

-- Olayın zamanı = COALESCE(OgrenciGTarih, OgrenciCTarih) (rapor, mobil liste ve SMS taraması bununla süzer/sıralar)
IF COL_LENGTH(N'OgrenciDetaylar', N'OlayZamani') IS NULL
BEGIN
    ALTER TABLE [OgrenciDetaylar] ADD [OlayZamani] AS COALESCE([OgrenciGTarih],[OgrenciCTarih]) PERSISTED;
    -- Kolon eklenirken bölünen sayfaları sıkıştır
    ALTER INDEX ALL ON [OgrenciDetaylar] REBUILD;
END;

-- Kapsayan indeks: tarama sorgusu tabloya gitmeden karşılanır (eksik tanımla kurulmuşsa yeniden kurulur)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OgrenciDetaylar_OlayZamani' AND object_id = OBJECT_ID(N'[OgrenciDetaylar]'))
   AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   WHERE i.name = N'IX_OgrenciDetaylar_OlayZamani' AND ic.is_included_column = 1 AND c.name = N'OgrenciSmsGonderildi')
    DROP INDEX [IX_OgrenciDetaylar_OlayZamani] ON [OgrenciDetaylar];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OgrenciDetaylar_OlayZamani' AND object_id = OBJECT_ID(N'[OgrenciDetaylar]'))
    CREATE INDEX [IX_OgrenciDetaylar_OlayZamani] ON [OgrenciDetaylar] ([OlayZamani])
        INCLUDE ([OgrenciId], [IstasyonTipi], [OgrenciSmsGonderildi], [OgrenciGTarih], [OgrenciCTarih]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OgrenciDetaylar_OgrenciId_OlayZamani' AND object_id = OBJECT_ID(N'[OgrenciDetaylar]'))
    CREATE INDEX [IX_OgrenciDetaylar_OgrenciId_OlayZamani] ON [OgrenciDetaylar] ([OgrenciId], [OlayZamani]);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SinifYoklamalar_OlusturulmaTarihi' AND object_id = OBJECT_ID(N'[SinifYoklamalar]'))
   AND NOT EXISTS (SELECT 1 FROM sys.index_columns ic JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                   WHERE i.name = N'IX_SinifYoklamalar_OlusturulmaTarihi' AND ic.is_included_column = 1 AND c.name = N'SmsDurumu')
    DROP INDEX [IX_SinifYoklamalar_OlusturulmaTarihi] ON [SinifYoklamalar];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SinifYoklamalar_OlusturulmaTarihi' AND object_id = OBJECT_ID(N'[SinifYoklamalar]'))
    CREATE INDEX [IX_SinifYoklamalar_OlusturulmaTarihi] ON [SinifYoklamalar] ([OlusturulmaTarihi])
        INCLUDE ([OgrenciId], [SmsDurumu], [Ders1], [Ders2], [Ders3], [Ders4], [Ders5], [Ders6], [Ders7], [Ders8]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ServisYoklamalar_OlusturulmaTarihi' AND object_id = OBJECT_ID(N'[ServisYoklamalar]'))
    CREATE INDEX [IX_ServisYoklamalar_OlusturulmaTarihi] ON [ServisYoklamalar] ([OlusturulmaTarihi]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bildirimler_Alici_Tarih' AND object_id = OBJECT_ID(N'[Bildirimler]'))
    CREATE INDEX [IX_Bildirimler_Alici_Tarih] ON [Bildirimler] ([AliciKullaniciId], [OlusturulmaTarihi]);

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261011151722_PerformansIndeksleri')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261011151722_PerformansIndeksleri', N'10.0.9');
