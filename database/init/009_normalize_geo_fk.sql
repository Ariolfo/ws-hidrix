/*
  Normaliza FKs geográficas en BD existente:
  - Ciudad solo → Departamento (quita Pais_Id / Depo_Cod)
  - Usuario solo → Ciudad (Ciu_Id)
  Autor: AGROSAVIA · Hidrix | 2026-08-07
*/
USE [dbHidrix];
GO

/* -------- Ciudad -------- */
IF COL_LENGTH('dbo.HidrtbCiudad', 'Pais_Id') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HidrtbCiudad_Pais')
        ALTER TABLE dbo.HidrtbCiudad DROP CONSTRAINT FK_HidrtbCiudad_Pais;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_HidrtbCiudad_Cod' AND object_id = OBJECT_ID(N'dbo.HidrtbCiudad'))
        ALTER TABLE dbo.HidrtbCiudad DROP CONSTRAINT UQ_HidrtbCiudad_Cod;

    ALTER TABLE dbo.HidrtbCiudad DROP COLUMN Pais_Id;
END
GO

IF COL_LENGTH('dbo.HidrtbCiudad', 'Depo_Cod') IS NOT NULL
BEGIN
    ALTER TABLE dbo.HidrtbCiudad DROP COLUMN Depo_Cod;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_HidrtbCiudad_Depo_Cod' AND object_id = OBJECT_ID(N'dbo.HidrtbCiudad'))
BEGIN
    ALTER TABLE dbo.HidrtbCiudad
        ADD CONSTRAINT UQ_HidrtbCiudad_Depo_Cod UNIQUE (Depo_Id, Ciu_Cod);
END
GO

-- HidrtbUsuario is managed by EF Core (ASP.NET Identity).
-- No manual schema changes needed here.
GO
