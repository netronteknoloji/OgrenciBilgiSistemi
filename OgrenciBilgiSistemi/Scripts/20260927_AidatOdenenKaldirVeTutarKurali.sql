BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    ALTER TABLE [OgrenciAidatOdemeler] DROP CONSTRAINT [CK_AidatOdeme_Tutar_NonNegative];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    ALTER TABLE [OgrenciAidatlar] DROP CONSTRAINT [CK_Aidat_Pozitif];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OgrenciAidatlar]') AND [c].[name] = N'Odenen');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [OgrenciAidatlar] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [OgrenciAidatlar] DROP COLUMN [Odenen];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OgrenciAidatlar]') AND [c].[name] = N'SonOdemeTarihi');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [OgrenciAidatlar] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [OgrenciAidatlar] DROP COLUMN [SonOdemeTarihi];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OgrenciAidatlar]') AND [c].[name] = N'Borc');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [OgrenciAidatlar] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [OgrenciAidatlar] ALTER COLUMN [Borc] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN

    UPDATE a SET a.Borc = NULL
    FROM OgrenciAidatlar a
    LEFT JOIN OgrenciAidatTarifeler t ON t.BaslangicYil = a.BaslangicYil
    WHERE a.Borc <= 0 OR a.Borc = t.Tutar;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    UPDATE OgrenciAidatOdemeler SET IsDeleted = 1 WHERE Tutar = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    EXEC(N'ALTER TABLE [OgrenciAidatOdemeler] ADD CONSTRAINT [CK_AidatOdeme_Tutar_Pozitif] CHECK ([Tutar] > 0 OR [IsDeleted] = 1)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    EXEC(N'ALTER TABLE [OgrenciAidatlar] ADD CONSTRAINT [CK_Aidat_Pozitif] CHECK ([Borc] IS NULL OR [Borc] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927092659_AidatOdenenKaldirVeTutarKurali'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260927092659_AidatOdenenKaldirVeTutarKurali', N'10.0.9');
END;

COMMIT;
GO

