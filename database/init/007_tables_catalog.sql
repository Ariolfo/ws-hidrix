/*
  Catálogo Red–País, Cultivos y metadatos de sensor (cultivo/CC).
  Autor: AGROSAVIA · Hidrix | 2026-09-15
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbRed', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbRed (
        Red_Id     INT IDENTITY(1,1) NOT NULL,
        Red_Nombre NVARCHAR(50)      NOT NULL,
        Pais_Id    INT               NOT NULL,
        CONSTRAINT PK_HidrtbRed PRIMARY KEY (Red_Id),
        CONSTRAINT UQ_HidrtbRed_Nombre UNIQUE (Red_Nombre),
        CONSTRAINT FK_HidrtbRed_Pais FOREIGN KEY (Pais_Id) REFERENCES dbo.HidrtbPais (Pais_Id)
    );
END
GO

IF OBJECT_ID(N'dbo.HidrtbCultivo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbCultivo (
        Cult_Id               INT IDENTITY(1,1) NOT NULL,
        Cult_Nombre           NVARCHAR(100)     NOT NULL,
        Cult_CapacidadCampo   DECIMAL(8,2)      NOT NULL,
        Cult_PorcentajeMaximo DECIMAL(8,2)      NOT NULL,
        Cult_DecisionRiego    DECIMAL(8,2)      NOT NULL,
        Cult_Activo           BIT               NOT NULL CONSTRAINT DF_HidrtbCultivo_Activo DEFAULT (1),
        Cult_FechaCreacion    DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbCultivo_Creacion DEFAULT (SYSUTCDATETIME()),
        Cult_FechaActualizacion DATETIME2(7)    NOT NULL CONSTRAINT DF_HidrtbCultivo_Actualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HidrtbCultivo PRIMARY KEY (Cult_Id),
        CONSTRAINT UQ_HidrtbCultivo_Nombre UNIQUE (Cult_Nombre)
    );
END
GO

IF OBJECT_ID(N'dbo.HidrtbSensorMeta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbSensorMeta (
        Meta_Id                 INT IDENTITY(1,1) NOT NULL,
        Sens_Nombre             NVARCHAR(50)      NOT NULL,
        Cult_Id                 INT               NULL,
        Sens_Finca              NVARCHAR(150)     NULL,
        Sens_CCEstimado         DECIMAL(5, 2)     NULL,
        Sens_MetodoCC           NVARCHAR(50)      NULL,
        Sens_FechaEstimacionCC  DATETIME2(7)      NULL,
        Meta_Activo             BIT               NOT NULL CONSTRAINT DF_HidrtbSensorMeta_Activo DEFAULT (1),
        Meta_FechaCreacion      DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbSensorMeta_Creacion DEFAULT (SYSUTCDATETIME()),
        Meta_FechaActualizacion DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbSensorMeta_Actualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HidrtbSensorMeta PRIMARY KEY (Meta_Id),
        CONSTRAINT UQ_HidrtbSensorMeta_Nombre UNIQUE (Sens_Nombre),
        CONSTRAINT FK_HidrtbSensorMeta_Cult FOREIGN KEY (Cult_Id) REFERENCES dbo.HidrtbCultivo (Cult_Id)
    );
END
GO
