/*
  CC estimada por sensor (método SWDP / R-SAX).
  Autor: AGROSAVIA · Hidrix | 2026-08-31
*/
USE [dbHidrix];
GO

IF COL_LENGTH(N'dbo.HidrtbSensor', N'Sens_CCEstimado') IS NULL
BEGIN
    ALTER TABLE dbo.HidrtbSensor
        ADD Sens_CCEstimado DECIMAL(5, 2) NULL;
END
GO

IF COL_LENGTH(N'dbo.HidrtbSensor', N'Sens_MetodoCC') IS NULL
BEGIN
    ALTER TABLE dbo.HidrtbSensor
        ADD Sens_MetodoCC NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH(N'dbo.HidrtbSensor', N'Sens_FechaEstimacionCC') IS NULL
BEGIN
    ALTER TABLE dbo.HidrtbSensor
        ADD Sens_FechaEstimacionCC DATETIME2(7) NULL;
END
GO
