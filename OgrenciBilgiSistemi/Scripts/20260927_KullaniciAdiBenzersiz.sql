BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927111253_KullaniciAdiBenzersiz'
)
BEGIN

    UPDATE K SET K.IsDeleted = 1
    FROM Kullanicilar K
    INNER JOIN OgretmenProfiller OP ON OP.KullaniciId = K.KullaniciId
    WHERE K.Rol = 2 AND OP.IsDeleted = 1 AND K.IsDeleted = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927111253_KullaniciAdiBenzersiz'
)
BEGIN

    IF EXISTS (SELECT 1 FROM Kullanicilar WHERE IsDeleted = 0 GROUP BY KullaniciAdi HAVING COUNT(*) > 1)
        THROW 50001, N'Aktif kullanıcılar arasında mükerrer KullaniciAdi var. Önce bu kayıtları yeniden adlandırın.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927111253_KullaniciAdiBenzersiz'
)
BEGIN
    DROP INDEX [IX_Kullanicilar_KullaniciAdi] ON [Kullanicilar];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927111253_KullaniciAdiBenzersiz'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Kullanicilar_KullaniciAdi] ON [Kullanicilar] ([KullaniciAdi]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927111253_KullaniciAdiBenzersiz'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260927111253_KullaniciAdiBenzersiz', N'10.0.9');
END;

COMMIT;
GO

