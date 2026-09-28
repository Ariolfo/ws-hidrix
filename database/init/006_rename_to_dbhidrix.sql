/*
  Asegura collation Modern_Spanish_CI_AS en dbHidrix.
*/
SET NOCOUNT ON;

IF DB_ID(N'dbHidrix') IS NULL
BEGIN
    PRINT N'dbHidrix no existe; ejecutar 001_create_database.sql.';
END
ELSE IF EXISTS (
    SELECT 1 FROM sys.databases
    WHERE name = N'dbHidrix' AND collation_name = N'Modern_Spanish_CI_AS'
)
BEGIN
    PRINT N'dbHidrix ya tiene collation Modern_Spanish_CI_AS.';
END
ELSE
BEGIN
    ALTER DATABASE [dbHidrix] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ALTER DATABASE [dbHidrix] COLLATE Modern_Spanish_CI_AS;
    ALTER DATABASE [dbHidrix] SET MULTI_USER;
    PRINT N'Collation de dbHidrix actualizada a Modern_Spanish_CI_AS.';
END
GO
