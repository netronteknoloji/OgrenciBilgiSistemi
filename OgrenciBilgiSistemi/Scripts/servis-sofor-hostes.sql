-- ServisSoforHostesBilgileri migration'ının elle uygulanabilir karşılığı.
-- Tekrar çalıştırılabilir; her okul DB'sinde çalıştırın.
IF COL_LENGTH('ServisProfiller', 'SoforAdSoyad') IS NULL
    ALTER TABLE ServisProfiller ADD SoforAdSoyad nvarchar(100) NULL;
IF COL_LENGTH('ServisProfiller', 'SoforTcKimlikNo') IS NULL
    ALTER TABLE ServisProfiller ADD SoforTcKimlikNo nvarchar(11) NULL;
IF COL_LENGTH('ServisProfiller', 'HostesAdSoyad') IS NULL
    ALTER TABLE ServisProfiller ADD HostesAdSoyad nvarchar(100) NULL;
IF COL_LENGTH('ServisProfiller', 'HostesTcKimlikNo') IS NULL
    ALTER TABLE ServisProfiller ADD HostesTcKimlikNo nvarchar(11) NULL;
IF COL_LENGTH('ServisProfiller', 'HostesTelefon') IS NULL
    ALTER TABLE ServisProfiller ADD HostesTelefon nvarchar(15) NULL;
