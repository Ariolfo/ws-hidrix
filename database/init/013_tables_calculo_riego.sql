/*
  Registros de la calculadora de riego por usuario.
  Convención DTI Hidrix: Hidrtb* / Prefijo_Campo
  Autor: AGROSAVIA · Hidrix | 2026-08-12
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbCalculoRiego', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbCalculoRiego (
        Calc_Id                 INT IDENTITY(1,1) NOT NULL,
        Usua_Id                 NVARCHAR(450)     NOT NULL,
        Calc_CultivoNombre      NVARCHAR(100)     NOT NULL,
        Cult_Id                 INT               NULL,
        Calc_CapacidadCampo     DECIMAL(8,2)      NOT NULL,
        Calc_LimiteMaxRiego     DECIMAL(8,2)      NOT NULL,
        Calc_DecisionRiego      DECIMAL(8,2)      NOT NULL,
        Calc_FechaConsulta      DATE              NOT NULL,
        Calc_HumedadManana      DECIMAL(8,2)      NOT NULL,
        Calc_HumedadTarde       DECIMAL(8,2)      NOT NULL,
        Calc_Recomendacion      NVARCHAR(20)      NOT NULL,
        Calc_RealizoRiego       NVARCHAR(10)      NULL,
        Calc_Observacion        NVARCHAR(500)     NULL,
        Calc_Activo             BIT               NOT NULL CONSTRAINT DF_HidrtbCalculoRiego_Activo DEFAULT (1),
        Calc_FechaCreacion      DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbCalculoRiego_Creacion DEFAULT (SYSUTCDATETIME()),
        Calc_FechaActualizacion DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbCalculoRiego_Actualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HidrtbCalculoRiego PRIMARY KEY (Calc_Id),
        CONSTRAINT FK_HidrtbCalculoRiego_Usuario FOREIGN KEY (Usua_Id)
            REFERENCES dbo.HidrtbUsuario (Usua_Id),
        CONSTRAINT FK_HidrtbCalculoRiego_Cultivo FOREIGN KEY (Cult_Id)
            REFERENCES dbo.HidrtbCultivo (Cult_Id)
    );

    CREATE INDEX IX_HidrtbCalculoRiego_Usua_Cultivo
        ON dbo.HidrtbCalculoRiego (Usua_Id, Calc_CultivoNombre, Calc_FechaConsulta DESC);
END
GO
