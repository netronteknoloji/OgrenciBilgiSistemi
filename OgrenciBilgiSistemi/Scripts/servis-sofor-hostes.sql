-- ServisSoforHostesBilgileri migration'ının elle uygulanabilir karşılığı.
-- Tekrar çalıştırılabilir; her okul DB'sinde çalıştırın.
IF COL_LENGTH('ServisProfiller', 'SoforAdSoyad') IS NULL
    ALTER TABLE ServisProfiller ADD SoforAdSoyad nvarchar(100) NULL;
IF COL_LENGTH('ServisProfiller', 'SoforTcKimlikNo') IS NULL
    ALTER TABLE ServisProfiller ADD SoforTcKimlikNo nvarchar(11) NULL;
IF COL_LENGTH('ServisProfiller', 'HostesAdSoyad') IS NULL
    ALTER TABLE ServisProfiller ADD HostesAdSoyad nvarchar(100) NULL;
IF COL_LENGTH('ServisProfiller', 'HostesTcKimlikNo') IS NULL
    ALTER TABLE ServisProfiller ADD HostesTcKimlikNo nvarchar(11) NULL;
IF COL_LENGTH('ServisProfiller', 'HostesTelefon') IS NULL
    ALTER TABLE ServisProfiller ADD HostesTelefon nvarchar(15) NULL;

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261011083142_ServisSoforHostesBilgileri')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261011083142_ServisSoforHostesBilgileri', N'10.0.9');
