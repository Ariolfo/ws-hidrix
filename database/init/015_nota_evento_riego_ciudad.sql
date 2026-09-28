/*
  Lugar de nota evento riego: ciudad del catálogo (reemplaza GPS opcional).
  Autor: AGROSAVIA · Hidrix | 2026-08-24
*/
USE [dbHidrix];
GO

IF COL_LENGTH(N'dbo.HidrtbNotaEventoRiego', N'Ciu_Id') IS NULL
BEGIN
    ALTER TABLE dbo.HidrtbNotaEventoRiego
        ADD Ciu_Id INT NULL;

    ALTER TABLE dbo.HidrtbNotaEventoRiego
        ADD CONSTRAINT FK_HidrtbNotaEventoRiego_Ciudad
            FOREIGN KEY (Ciu_Id) REFERENCES dbo.HidrtbCiudad (Ciu_Id);
END
GO
