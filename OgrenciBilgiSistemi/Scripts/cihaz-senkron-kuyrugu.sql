-- CihazSenkronKuyrugu migration'ının elle uygulanabilir karşılığı (öğrenci ↔ ZKTeco cihaz senkron outbox'ı).
-- Tekrar çalıştırılabilir; her okul DB'sinde, yeni sürüm yayınlanmadan ÖNCE çalıştırın.
-- Filtreli unique index için gerekli (sqlcmd varsayılanı OFF)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF OBJECT_ID(N'[CihazSenkronKuyrugu]') IS NULL
BEGIN
    CREATE TABLE [CihazSenkronKuyrugu] (
        [CihazSenkronKuyruguId] bigint NOT NULL IDENTITY(1, 1),
        [CihazId] int NOT NULL,
        [OgrenciId] int NOT NULL,
        [Durum] tinyint NOT NULL,
        [SonIslem] tinyint NOT NULL,
        [DenemeSayisi] int NOT NULL,
        [SonrakiDeneme] datetime2 NOT NULL,
        [KilitBitis] datetime2 NULL,
        [OlusturmaZamani] datetime2 NOT NULL,
        [TamamlanmaZamani] datetime2 NULL,
        [SonHata] nvarchar(500) NULL,
        CONSTRAINT [PK_CihazSenkronKuyrugu] PRIMARY KEY ([CihazSenkronKuyruguId]),
        CONSTRAINT [FK_CihazSenkronKuyrugu_Cihazlar_CihazId] FOREIGN KEY ([CihazId]) REFERENCES [Cihazlar] ([CihazId]),
        CONSTRAINT [FK_CihazSenkronKuyrugu_Ogrenciler_OgrenciId] FOREIGN KEY ([OgrenciId]) REFERENCES [Ogrenciler] ([OgrenciId])
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CihazSenkronKuyrugu_Cihaz_Durum' AND object_id = OBJECT_ID(N'[CihazSenkronKuyrugu]'))
    CREATE INDEX [IX_CihazSenkronKuyrugu_Cihaz_Durum] ON [CihazSenkronKuyrugu] ([CihazId], [Durum]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CihazSenkronKuyrugu_Durum_SonrakiDeneme' AND object_id = OBJECT_ID(N'[CihazSenkronKuyrugu]'))
    CREATE INDEX [IX_CihazSenkronKuyrugu_Durum_SonrakiDeneme] ON [CihazSenkronKuyrugu] ([Durum], [SonrakiDeneme]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CihazSenkronKuyrugu_OgrenciId' AND object_id = OBJECT_ID(N'[CihazSenkronKuyrugu]'))
    CREATE INDEX [IX_CihazSenkronKuyrugu_OgrenciId] ON [CihazSenkronKuyrugu] ([OgrenciId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CihazSenkronKuyrugu_Bekleyen' AND object_id = OBJECT_ID(N'[CihazSenkronKuyrugu]'))
    CREATE UNIQUE INDEX [UX_CihazSenkronKuyrugu_Bekleyen] ON [CihazSenkronKuyrugu] ([CihazId], [OgrenciId]) WHERE [Durum] = 0;

-- Migration geçmişine kayıt: sonraki EF idempotent betiği bu adımı tekrar denemesin.
-- Yalnızca EF ile yönetilen DB'lerde (InitialCreate kaydı olan) yazılır; geçmişi boş DB'de sıra bozulmasın diye atlanır.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260701083716_InitialCreate')
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261012182013_CihazSenkronKuyrugu')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261012182013_CihazSenkronKuyrugu', N'10.0.9');
