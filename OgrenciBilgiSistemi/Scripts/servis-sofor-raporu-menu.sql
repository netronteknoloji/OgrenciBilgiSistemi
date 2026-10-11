-- ServisSoforRaporuMenu migration'ının elle uygulanabilir karşılığı.
-- Tekrar çalıştırılabilir; her okul DB'sinde çalıştırın.
-- Raporlar > Servis Şoför Raporu menü öğesi (Id 35). Admin dışındaki kullanıcılara
-- yetki Kullanıcı İşlemleri ekranından verilir.
IF NOT EXISTS (SELECT 1 FROM MenuOgeler WHERE Id = 35)
BEGIN
    IF OBJECTPROPERTY(OBJECT_ID('MenuOgeler'), 'TableHasIdentity') = 1
        SET IDENTITY_INSERT MenuOgeler ON;

    INSERT INTO MenuOgeler (Id, [Action], AnaMenuId, Baslik, [Controller], GerekliRole, Sirala)
    VALUES (35, N'Index', 18, N'Servis Şoför Raporu', N'ServisRapor', NULL, 6);

    IF OBJECTPROPERTY(OBJECT_ID('MenuOgeler'), 'TableHasIdentity') = 1
        SET IDENTITY_INSERT MenuOgeler OFF;
END;

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261011092335_ServisSoforRaporuMenu')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261011092335_ServisSoforRaporuMenu', N'10.0.9');
