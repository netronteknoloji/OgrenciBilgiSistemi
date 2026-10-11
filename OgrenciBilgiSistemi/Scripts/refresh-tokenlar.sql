-- RefreshTokenlar migration'ının elle uygulanabilir karşılığı (mobil oturumlar DB'de).
-- Tekrar çalıştırılabilir; her okul DB'sinde, yeni sürüm yayınlanmadan ÖNCE çalıştırın.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'[RefreshTokenlar]') IS NULL
BEGIN
    CREATE TABLE [RefreshTokenlar] (
        [RefreshTokenId] bigint NOT NULL IDENTITY(1, 1),
        [KullaniciId] int NOT NULL,
        [OturumId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [OlusturmaZamani] datetime2 NOT NULL,
        [SonKullanim] datetime2 NOT NULL,
        [IptalZamani] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokenlar] PRIMARY KEY ([RefreshTokenId])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokenlar_KullaniciId' AND object_id = OBJECT_ID(N'[RefreshTokenlar]'))
    CREATE INDEX [IX_RefreshTokenlar_KullaniciId] ON [RefreshTokenlar] ([KullaniciId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokenlar_OturumId' AND object_id = OBJECT_ID(N'[RefreshTokenlar]'))
    CREATE INDEX [IX_RefreshTokenlar_OturumId] ON [RefreshTokenlar] ([OturumId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_RefreshTokenlar_TokenHash' AND object_id = OBJECT_ID(N'[RefreshTokenlar]'))
    CREATE UNIQUE INDEX [UX_RefreshTokenlar_TokenHash] ON [RefreshTokenlar] ([TokenHash]);

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261011131723_RefreshTokenlar')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261011131723_RefreshTokenlar', N'10.0.9');
