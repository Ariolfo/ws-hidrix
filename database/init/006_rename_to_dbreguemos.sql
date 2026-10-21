/*
  Asegura collation Modern_Spanish_CI_AS en dbReguemos.
*/
SET NOCOUNT ON;

IF DB_ID(N'dbReguemos') IS NULL
BEGIN
    PRINT N'dbReguemos no existe; ejecutar 001_create_database.sql.';
END
ELSE IF EXISTS (
    SELECT 1 FROM sys.databases
    WHERE name = N'dbReguemos' AND collation_name = N'Modern_Spanish_CI_AS'
)
BEGIN
    PRINT N'dbReguemos ya tiene collation Modern_Spanish_CI_AS.';
END
ELSE
BEGIN
    ALTER DATABASE [dbReguemos] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ALTER DATABASE [dbReguemos] COLLATE Modern_Spanish_CI_AS;
    ALTER DATABASE [dbReguemos] SET MULTI_USER;
    PRINT N'Collation de dbReguemos actualizada a Modern_Spanish_CI_AS.';
END
GO
