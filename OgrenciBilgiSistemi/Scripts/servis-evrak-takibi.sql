-- ServisEvrakTakibi migration'ının elle uygulanabilir karşılığı.
-- Tekrar çalıştırılabilir; her okul DB'sinde çalıştırın.
IF COL_LENGTH('ServisProfiller', 'EvrakTamMi') IS NULL
    ALTER TABLE ServisProfiller ADD EvrakTamMi bit NOT NULL CONSTRAINT DF_ServisProfiller_EvrakTamMi DEFAULT 0;
IF COL_LENGTH('ServisProfiller', 'EksikEvraklar') IS NULL
    ALTER TABLE ServisProfiller ADD EksikEvraklar nvarchar(500) NULL;

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261011090742_ServisEvrakTakibi')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261011090742_ServisEvrakTakibi', N'10.0.9');
