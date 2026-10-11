-- ServisEvrakTakibi migration'ının elle uygulanabilir karşılığı.
-- Tekrar çalıştırılabilir; her okul DB'sinde çalıştırın.
IF COL_LENGTH('ServisProfiller', 'EvrakTamMi') IS NULL
    ALTER TABLE ServisProfiller ADD EvrakTamMi bit NOT NULL CONSTRAINT DF_ServisProfiller_EvrakTamMi DEFAULT 0;
IF COL_LENGTH('ServisProfiller', 'EksikEvraklar') IS NULL
    ALTER TABLE ServisProfiller ADD EksikEvraklar nvarchar(500) NULL;
