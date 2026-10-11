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
