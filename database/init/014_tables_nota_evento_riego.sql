/*
  Notas de evento de riego por usuario (cliente).
  Convención DTI Hidrix: Hidrtb* / Evri_*
  Autor: AGROSAVIA · Hidrix | 2026-08-24
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbNotaEventoRiego', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbNotaEventoRiego (
        Evri_Id                 INT IDENTITY(1,1) NOT NULL,
        Usua_Id                 NVARCHAR(450)     NOT NULL,
        Evri_NombreParcelaLote  NVARCHAR(120)     NOT NULL,
        Evri_Cultivo            NVARCHAR(100)     NOT NULL,
        Cult_Id                 INT               NULL,
        Evri_Fecha              DATE              NOT NULL,
        Evri_HoraInicio         TIME(0)           NOT NULL,
        Evri_HoraFin            TIME(0)           NOT NULL,
        Evri_DuracionMinutos    INT               NOT NULL,
        Evri_TipoRiego          NVARCHAR(30)      NOT NULL,
        Evri_CaudalHoraLph      DECIMAL(10,2)     NULL,
        Evri_TipoSuelo          NVARCHAR(80)      NULL,
        Evri_LugarLatitud       DECIMAL(9,6)      NULL,
        Evri_LugarLongitud      DECIMAL(9,6)      NULL,
        Evri_Activo             BIT               NOT NULL CONSTRAINT DF_HidrtbNotaEventoRiego_Activo DEFAULT (1),
        Evri_FechaCreacion      DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbNotaEventoRiego_Creacion DEFAULT (SYSUTCDATETIME()),
        Evri_FechaActualizacion DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbNotaEventoRiego_Actualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HidrtbNotaEventoRiego PRIMARY KEY (Evri_Id),
        CONSTRAINT FK_HidrtbNotaEventoRiego_Usuario FOREIGN KEY (Usua_Id)
            REFERENCES dbo.HidrtbUsuario (Usua_Id),
        CONSTRAINT FK_HidrtbNotaEventoRiego_Cultivo FOREIGN KEY (Cult_Id)
            REFERENCES dbo.HidrtbCultivo (Cult_Id),
        CONSTRAINT CK_HidrtbNotaEventoRiego_TipoRiego CHECK (
            Evri_TipoRiego IN (
                N'goteo_terrestre',
                N'subterraneo',
                N'microaspersion',
                N'aspersion',
                N'manual'
            )
        ),
        CONSTRAINT CK_HidrtbNotaEventoRiego_Duracion CHECK (Evri_DuracionMinutos >= 0 AND Evri_DuracionMinutos <= 1440)
    );

    CREATE INDEX IX_HidrtbNotaEventoRiego_Usua_Fecha
        ON dbo.HidrtbNotaEventoRiego (Usua_Id, Evri_Fecha DESC, Evri_Id DESC);
END
GO
