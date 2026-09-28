/*
  CC estimada por sensor (legado HidrtbSensor; vigente en HidrtbSensorMeta vía 017).
  Autor: AGROSAVIA · Hidrix | 2026-08-31
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbSensor', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.HidrtbSensor', N'Sens_CCEstimado') IS NULL
    BEGIN
        ALTER TABLE dbo.HidrtbSensor
            ADD Sens_CCEstimado DECIMAL(5, 2) NULL;
    END

    IF COL_LENGTH(N'dbo.HidrtbSensor', N'Sens_MetodoCC') IS NULL
    BEGIN
        ALTER TABLE dbo.HidrtbSensor
            ADD Sens_MetodoCC NVARCHAR(50) NULL;
    END

    IF COL_LENGTH(N'dbo.HidrtbSensor', N'Sens_FechaEstimacionCC') IS NULL
    BEGIN
        ALTER TABLE dbo.HidrtbSensor
            ADD Sens_FechaEstimacionCC DATETIME2(7) NULL;
    END
END
GO
