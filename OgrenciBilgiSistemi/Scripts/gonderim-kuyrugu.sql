-- GonderimKuyrugu migration'ının elle uygulanabilir karşılığı (SMS + push outbox).
-- Tekrar çalıştırılabilir; her okul DB'sinde, yeni sürüm yayınlanmadan ÖNCE çalıştırın.
-- Filtreli unique index için gerekli (sqlcmd varsayılanı OFF)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF OBJECT_ID(N'[GonderimKuyrugu]') IS NULL
BEGIN
    CREATE TABLE [GonderimKuyrugu] (
        [GonderimKuyruguId] bigint NOT NULL IDENTITY(1, 1),
        [Kanal] tinyint NOT NULL,
        [Tip] nvarchar(50) NOT NULL,
        [TekilAnahtar] nvarchar(100) NULL,
        [OgrenciId] int NULL,
        [Telefon] nvarchar(20) NULL,
        [Mesaj] nvarchar(500) NULL,
        [AliciKullaniciId] int NULL,
        [BildirimId] int NULL,
        [Durum] tinyint NOT NULL,
        [DenemeSayisi] int NOT NULL,
        [SonrakiDeneme] datetime2 NOT NULL,
        [KilitBitis] datetime2 NULL,
        [GecerlilikBitis] datetime2 NOT NULL,
        [OlusturmaZamani] datetime2 NOT NULL,
        [GonderimZamani] datetime2 NULL,
        [SonHata] nvarchar(500) NULL,
        CONSTRAINT [PK_GonderimKuyrugu] PRIMARY KEY ([GonderimKuyruguId]),
        CONSTRAINT [FK_GonderimKuyrugu_Bildirimler_BildirimId] FOREIGN KEY ([BildirimId]) REFERENCES [Bildirimler] ([BildirimId])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GonderimKuyrugu_Durum_SonrakiDeneme' AND object_id = OBJECT_ID(N'[GonderimKuyrugu]'))
    CREATE INDEX [IX_GonderimKuyrugu_Durum_SonrakiDeneme] ON [GonderimKuyrugu] ([Durum], [SonrakiDeneme]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_GonderimKuyrugu_BildirimId' AND object_id = OBJECT_ID(N'[GonderimKuyrugu]'))
    CREATE UNIQUE INDEX [UX_GonderimKuyrugu_BildirimId] ON [GonderimKuyrugu] ([BildirimId]) WHERE [BildirimId] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_GonderimKuyrugu_TekilAnahtar' AND object_id = OBJECT_ID(N'[GonderimKuyrugu]'))
    CREATE UNIQUE INDEX [UX_GonderimKuyrugu_TekilAnahtar] ON [GonderimKuyrugu] ([TekilAnahtar]) WHERE [TekilAnahtar] IS NOT NULL;

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261011130339_GonderimKuyrugu')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261011130339_GonderimKuyrugu', N'10.0.9');
