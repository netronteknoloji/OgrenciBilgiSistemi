-- Rol / ilişki veri temizliği (rol analizi 2026-10-12).
-- Tekrar çalıştırılabilir. Her okul DB'sinde, ÖNCE YEDEK ALARAK çalıştırın.
-- @SadeceRapor = 1 ile yalnızca sayıları gösterir, hiçbir şey değiştirmez.
-- Kişisel veri yazdırmaz (yalnızca sayı, öğrenci no ve kullanıcı Id).
-- sqlcmd ile: sqlcmd -S <sunucu> -d <okulDb> -E -C -f 65001 -i 20261012_rol-iliski-temizligi.sql  (-f 65001: Türkçe karakterler)
-- Sonda listelenen öğrencilere yeni veli, profili olmayan kullanıcılara profil panelden atanmalı.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @SadeceRapor bit = 0;

-- Öğrencinin olması gereken öğretmeni: sınıfın aktif, rolü Öğretmen olan, en küçük Id'li öğretmeni
-- (OgrenciBilgiSistemi/Helpers/OgretmenAtamaHelper.cs ile aynı kural)
IF OBJECT_ID('tempdb..#BeklenenOgretmen') IS NOT NULL DROP TABLE #BeklenenOgretmen;
SELECT O.OgrenciId,
       (SELECT TOP 1 OP.KullaniciId
          FROM OgretmenProfiller OP
          INNER JOIN Kullanicilar K ON K.KullaniciId = OP.KullaniciId
         WHERE OP.BirimId = O.BirimId AND OP.IsDeleted = 0 AND K.IsDeleted = 0 AND K.Rol = 2
         ORDER BY OP.KullaniciId) AS OgretmenId
INTO #BeklenenOgretmen
FROM Ogrenciler O;

PRINT N'--- Ön rapor ---';
SELECT N'OgretmenId var olmayan kullanıcıyı gösteriyor' AS Kontrol, COUNT(*) AS Adet
  FROM Ogrenciler O WHERE O.OgretmenId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM Kullanicilar K WHERE K.KullaniciId = O.OgretmenId)
UNION ALL
SELECT N'VeliId var olmayan kullanıcıyı gösteriyor', COUNT(*)
  FROM Ogrenciler O WHERE O.VeliId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM Kullanicilar K WHERE K.KullaniciId = O.VeliId)
UNION ALL
SELECT N'OgretmenId rolü Öğretmen olmayan kullanıcıyı gösteriyor', COUNT(*)
  FROM Ogrenciler O INNER JOIN Kullanicilar K ON K.KullaniciId = O.OgretmenId WHERE K.Rol <> 2
UNION ALL
SELECT N'OgretmenId sınıf kuralından farklı (düzeltilecek)', COUNT(*)
  FROM Ogrenciler O INNER JOIN #BeklenenOgretmen B ON B.OgrenciId = O.OgrenciId
 WHERE ISNULL(O.OgretmenId, -1) <> ISNULL(B.OgretmenId, -1)
UNION ALL
SELECT N'Rolüyle uyuşmayan aktif öğretmen profili', COUNT(*)
  FROM OgretmenProfiller P INNER JOIN Kullanicilar K ON K.KullaniciId = P.KullaniciId
 WHERE P.IsDeleted = 0 AND K.Rol <> 2
UNION ALL
SELECT N'Rolüyle uyuşmayan aktif servis profili', COUNT(*)
  FROM ServisProfiller P INNER JOIN Kullanicilar K ON K.KullaniciId = P.KullaniciId
 WHERE P.IsDeleted = 0 AND K.Rol <> 3
UNION ALL
SELECT N'Rolüyle uyuşmayan aktif veli profili', COUNT(*)
  FROM VeliProfiller P INNER JOIN Kullanicilar K ON K.KullaniciId = P.KullaniciId
 WHERE P.IsDeleted = 0 AND K.Rol <> 4
UNION ALL
SELECT N'Profili olmayan aktif öğretmen/servis/veli (elle düzeltilmeli)', COUNT(*)
  FROM Kullanicilar K
 WHERE K.IsDeleted = 0
   AND ((K.Rol = 2 AND NOT EXISTS (SELECT 1 FROM OgretmenProfiller P WHERE P.KullaniciId = K.KullaniciId))
     OR (K.Rol = 3 AND NOT EXISTS (SELECT 1 FROM ServisProfiller P WHERE P.KullaniciId = K.KullaniciId))
     OR (K.Rol = 4 AND NOT EXISTS (SELECT 1 FROM VeliProfiller P WHERE P.KullaniciId = K.KullaniciId)))
UNION ALL
SELECT N'Aktif kullanıcılarda tekrar eden kullanıcı adı', COUNT(*)
  FROM (SELECT KullaniciAdi FROM Kullanicilar WHERE IsDeleted = 0 GROUP BY KullaniciAdi HAVING COUNT(*) > 1) T
UNION ALL
SELECT N'Güvenilmeyen (untrusted) FK: ' + name, 1
  FROM sys.foreign_keys
 WHERE parent_object_id = OBJECT_ID(N'Ogrenciler') AND is_not_trusted = 1;

-- Elle düzeltilecekler (Id/no listesi; isim yazdırılmaz)
SELECT N'Profili olmayan kullanıcı' AS Liste, K.KullaniciId, K.Rol
  FROM Kullanicilar K
 WHERE K.IsDeleted = 0
   AND ((K.Rol = 2 AND NOT EXISTS (SELECT 1 FROM OgretmenProfiller P WHERE P.KullaniciId = K.KullaniciId))
     OR (K.Rol = 3 AND NOT EXISTS (SELECT 1 FROM ServisProfiller P WHERE P.KullaniciId = K.KullaniciId))
     OR (K.Rol = 4 AND NOT EXISTS (SELECT 1 FROM VeliProfiller P WHERE P.KullaniciId = K.KullaniciId)));

SELECT N'Velisi silinecek öğrenci (yeni veli atanmalı)' AS Liste, O.OgrenciNo
  FROM Ogrenciler O
 WHERE O.VeliId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Kullanicilar K WHERE K.KullaniciId = O.VeliId);

IF @SadeceRapor = 1
BEGIN
    PRINT N'Yalnızca rapor: değişiklik yapılmadı.';
    RETURN;
END;

BEGIN TRANSACTION;

-- 1) OgretmenId'yi sınıf kuralına göre yeniden hesapla
UPDATE O SET O.OgretmenId = B.OgretmenId
  FROM Ogrenciler O INNER JOIN #BeklenenOgretmen B ON B.OgrenciId = O.OgrenciId
 WHERE ISNULL(O.OgretmenId, -1) <> ISNULL(B.OgretmenId, -1);
PRINT CONCAT(N'OgretmenId düzeltildi: ', @@ROWCOUNT);

-- 2) Var olmayan veliye bağlı öğrencilerin VeliId'si boşaltılır (yukarıdaki listeden yeniden atanır)
UPDATE O SET O.VeliId = NULL
  FROM Ogrenciler O
 WHERE O.VeliId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Kullanicilar K WHERE K.KullaniciId = O.VeliId);
PRINT CONCAT(N'Kayıp VeliId boşaltıldı: ', @@ROWCOUNT);

-- 3) Rolüyle uyuşmayan profiller pasif
UPDATE P SET P.IsDeleted = 1 FROM OgretmenProfiller P INNER JOIN Kullanicilar K ON K.KullaniciId = P.KullaniciId
 WHERE P.IsDeleted = 0 AND K.Rol <> 2;
PRINT CONCAT(N'Pasife alınan öğretmen profili: ', @@ROWCOUNT);
UPDATE P SET P.IsDeleted = 1 FROM ServisProfiller P INNER JOIN Kullanicilar K ON K.KullaniciId = P.KullaniciId
 WHERE P.IsDeleted = 0 AND K.Rol <> 3;
PRINT CONCAT(N'Pasife alınan servis profili: ', @@ROWCOUNT);
UPDATE P SET P.IsDeleted = 1 FROM VeliProfiller P INNER JOIN Kullanicilar K ON K.KullaniciId = P.KullaniciId
 WHERE P.IsDeleted = 0 AND K.Rol <> 4;
PRINT CONCAT(N'Pasife alınan veli profili: ', @@ROWCOUNT);

COMMIT TRANSACTION;

-- 4) FK'leri yeniden doğrula (veri düzeldiği için artık geçer; güvenilir olur)
ALTER TABLE [Ogrenciler] WITH CHECK CHECK CONSTRAINT [FK_Ogrenciler_Kullanicilar_OgretmenId];
ALTER TABLE [Ogrenciler] WITH CHECK CHECK CONSTRAINT [FK_Ogrenciler_Kullanicilar_VeliId];
PRINT N'FK''ler doğrulandı.';

-- 5) Kullanıcı adı tekilliği (aktif kullanıcılar arasında); tekrar varsa atlanır ve uyarılır
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Kullanicilar_KullaniciAdi'
             AND object_id = OBJECT_ID(N'Kullanicilar') AND (is_unique = 0 OR has_filter = 0))
BEGIN
    IF EXISTS (SELECT 1 FROM Kullanicilar WHERE IsDeleted = 0 GROUP BY KullaniciAdi HAVING COUNT(*) > 1)
        PRINT N'UYARI: Tekrar eden kullanıcı adı var; tekil index eklenmedi. Önce yeniden adlandırın.';
    ELSE
    BEGIN
        DROP INDEX [IX_Kullanicilar_KullaniciAdi] ON [Kullanicilar];
        EXEC(N'CREATE UNIQUE INDEX [IX_Kullanicilar_KullaniciAdi] ON [Kullanicilar] ([KullaniciAdi]) WHERE [IsDeleted] = 0');
        PRINT N'Kullanıcı adı tekil index eklendi.';
    END;
END;

DROP TABLE #BeklenenOgretmen;
