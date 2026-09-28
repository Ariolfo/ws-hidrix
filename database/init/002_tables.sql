/*
  Esquema Hidrtb* — nomenclatura PAUTAS_BD / Guía de Arquitectura AGROSAVIA.
  Geo: País → Departamento → Ciudad (sin FK redundantes).
  Usuario referencia solo Ciudad.
  Autor: AGROSAVIA · Hidrix | 2026-08-07
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbPais', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbPais (
        Pais_Id     INT           NOT NULL,
        Pais_Nombre NVARCHAR(255) NOT NULL,
        Pais_Estado INT           NOT NULL CONSTRAINT DF_HidrtbPais_Estado DEFAULT (2),
        CONSTRAINT PK_HidrtbPais PRIMARY KEY (Pais_Id)
    );
END
GO

IF OBJECT_ID(N'dbo.HidrtbDepartamento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbDepartamento (
        Depo_Id     INT           NOT NULL,
        Pais_Id     INT           NOT NULL,
        Depo_Code   NVARCHAR(50)  NOT NULL,
        Depo_Nombre NVARCHAR(255) NOT NULL,
        CONSTRAINT PK_HidrtbDepartamento PRIMARY KEY (Depo_Id),
        CONSTRAINT FK_HidrtbDepartamento_Pais FOREIGN KEY (Pais_Id) REFERENCES dbo.HidrtbPais (Pais_Id),
        CONSTRAINT UQ_HidrtbDepartamento_Pais_Code UNIQUE (Pais_Id, Depo_Code)
    );
END
GO

IF OBJECT_ID(N'dbo.HidrtbCiudad', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbCiudad (
        Ciu_Id     INT           NOT NULL,
        Ciu_Nombre NVARCHAR(255) NOT NULL,
        Depo_Id    INT           NOT NULL,
        Ciu_Cod    NVARCHAR(50)  NOT NULL,
        CONSTRAINT PK_HidrtbCiudad PRIMARY KEY (Ciu_Id),
        CONSTRAINT FK_HidrtbCiudad_Depo FOREIGN KEY (Depo_Id) REFERENCES dbo.HidrtbDepartamento (Depo_Id),
        CONSTRAINT UQ_HidrtbCiudad_Depo_Cod UNIQUE (Depo_Id, Ciu_Cod)
    );
END
GO

-- HidrtbUsuario is managed by EF Core (ASP.NET Identity / ApplicationUser).
-- It is created by 'dotnet ef database update' with nvarchar(450) PK.

-- Logs Serilog (misma BD; columnas estándar del sink MSSqlServer)
IF OBJECT_ID(N'dbo.HidrtbLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbLog (
        Id              INT IDENTITY(1,1) NOT NULL,
        Message         NVARCHAR(MAX) NULL,
        MessageTemplate NVARCHAR(MAX) NULL,
        Level           NVARCHAR(128) NULL,
        TimeStamp       DATETIME NOT NULL,
        Exception       NVARCHAR(MAX) NULL,
        Properties      NVARCHAR(MAX) NULL,
        CONSTRAINT PK_HidrtbLog PRIMARY KEY CLUSTERED (Id)
    );
    CREATE INDEX idx_HidrtbLog_TimeStamp ON dbo.HidrtbLog (TimeStamp);
    CREATE INDEX idx_HidrtbLog_Level ON dbo.HidrtbLog (Level);
END
GO
