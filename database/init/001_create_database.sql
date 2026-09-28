/*
  Crea la base de datos dbHidrix en SQL Server 2019.
  Collation corporativa DTI: Modern_Spanish_CI_AS
  Autor: AGROSAVIA · Hidrix
*/
IF DB_ID(N'dbHidrix') IS NULL
BEGIN
    CREATE DATABASE [dbHidrix]
        COLLATE Modern_Spanish_CI_AS;
END
GO
