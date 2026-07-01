/*
    IsDeleted DB Şema Doğrulama Script'i (SALT-OKUMA)
    --------------------------------------------------
    Amaç: *Durum -> IsDeleted birleştirme refactoru sonrası bir tenant DB'sinin
    migration açısından hazır olup olmadığını doğrular.

    API projesi ham SQL kullandığı için tablolarda 'IsDeleted' kolonu yoksa
    çalışma anında "Invalid column name 'IsDeleted'" hatası verir. Bu script
    hiçbir veriyi DEĞİŞTİRMEZ; yalnızca INFORMATION_SCHEMA üzerinden SELECT yapar.

    Not: Yalnızca 6 eski soft-delete kolon adı hedeflenir. Meşru domain kolonları
    (Randevular.Durum, DurumId, SmsDurumu, OgrenciCikisDurumu, HttpDurumKodu,
    KitapDurum) bilerek dahil edilmez -> yanlış alarm vermez.
*/

SET NOCOUNT ON;

-- =====================================================================
-- BÖLÜM 1: TEK DB KONTROLÜ
-- SSMS'te ilgili tenant DB seçili iken çalıştır.
-- =====================================================================

-- 1a) IsDeleted kolonu olan tablolar (migration UYGULANMIŞ olması beklenen tablolar)
SELECT DB_NAME()      AS Veritabani,
       c.TABLE_NAME   AS Tablo,
       'IsDeleted VAR' AS Durum
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.COLUMN_NAME = 'IsDeleted'
ORDER BY c.TABLE_NAME;

-- 1b) HÂLÂ eski soft-delete kolonu duran tablolar (BOŞ gelmeli — gelirse migration EKSİK)
SELECT DB_NAME()      AS Veritabani,
       c.TABLE_NAME   AS Tablo,
       c.COLUMN_NAME  AS EskiKolon,
       'MIGRATION EKSIK' AS Uyari
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.COLUMN_NAME IN
      ('OgrenciDurum','KullaniciDurum','OgretmenDurum',
       'VeliDurum','ServisDurum','BirimDurum')
ORDER BY c.TABLE_NAME;

-- 1c) Özet verdikt
SELECT DB_NAME() AS Veritabani,
       (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
         WHERE COLUMN_NAME = 'IsDeleted') AS IsDeletedKolonSayisi,
       (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
         WHERE COLUMN_NAME IN ('OgrenciDurum','KullaniciDurum','OgretmenDurum',
                               'VeliDurum','ServisDurum','BirimDurum')) AS EskiDurumKolonSayisi,
       CASE WHEN (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE COLUMN_NAME IN ('OgrenciDurum','KullaniciDurum','OgretmenDurum',
                                         'VeliDurum','ServisDurum','BirimDurum')) = 0
            THEN 'HAZIR (IsDeleted migration uygulanmis)'
            ELSE 'DIKKAT: eski Durum kolonlari duruyor -> API Invalid column name verir'
       END AS Verdikt;


-- =====================================================================
-- BÖLÜM 2: ÇOK DB TARAMA
-- appsettings.json -> Okullar[] içindeki DB adlarini asagidaki listeye yaz.
-- Tek çalıştırmada hepsini tarar. (Tüm DB'ler aynı SQL Server örneğinde olmalı.)
-- =====================================================================

SET NOCOUNT ON;

DECLARE @dbs TABLE (ad SYSNAME);
INSERT INTO @dbs (ad) VALUES
  (N'Okul1_DB'),          -- <-- gerçek tenant DB adlarıyla değiştir
  (N'Okul2_DB'),
  (N'Okul3_DB');

IF OBJECT_ID('tempdb..#sonuc') IS NOT NULL DROP TABLE #sonuc;
CREATE TABLE #sonuc
(
    Veritabani           SYSNAME,
    IsDeletedKolonSayisi INT,
    EskiDurumKolonSayisi INT
);

DECLARE @ad SYSNAME, @sql NVARCHAR(MAX);

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT ad FROM @dbs;
OPEN c;
FETCH NEXT FROM c INTO @ad;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@ad) IS NULL
    BEGIN
        INSERT INTO #sonuc (Veritabani, IsDeletedKolonSayisi, EskiDurumKolonSayisi)
        VALUES (@ad, -1, -1);   -- -1 = DB bulunamadi
    END
    ELSE
    BEGIN
        SET @sql = N'USE ' + QUOTENAME(@ad) + N';
            INSERT INTO #sonuc (Veritabani, IsDeletedKolonSayisi, EskiDurumKolonSayisi)
            SELECT DB_NAME(),
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = ''IsDeleted''),
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE COLUMN_NAME IN (''OgrenciDurum'',''KullaniciDurum'',''OgretmenDurum'',
                                        ''VeliDurum'',''ServisDurum'',''BirimDurum''));';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM c INTO @ad;
END
CLOSE c;
DEALLOCATE c;

SELECT Veritabani,
       IsDeletedKolonSayisi,
       EskiDurumKolonSayisi,
       CASE WHEN IsDeletedKolonSayisi = -1 THEN 'DB BULUNAMADI'
            WHEN EskiDurumKolonSayisi = 0  THEN 'HAZIR'
            ELSE 'DIKKAT: migration eksik'
       END AS Verdikt
FROM #sonuc
ORDER BY Veritabani;

DROP TABLE #sonuc;
