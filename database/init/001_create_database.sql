/*
  Crea la base de datos dbReguemos en SQL Server 2019.
  Collation corporativa DTI: Modern_Spanish_CI_AS
  Autor: AGROSAVIA · Reguemos
*/
IF DB_ID(N'dbReguemos') IS NULL
BEGIN
    CREATE DATABASE [dbReguemos]
        COLLATE Modern_Spanish_CI_AS;
END
GO
