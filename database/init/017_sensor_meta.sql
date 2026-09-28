/*
  Metadatos Hidrix por sensor Visualiti (cultivo, finca, CC).
  Migra desde HidrtbSensor y elimina el inventario SQL de sensores.
  Autor: AGROSAVIA · Hidrix | 2026-09-15
*/
USE [dbHidrix];
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

/* Migrar cultivo, finca y CC desde el inventario legado (antes de borrarlo). */
IF OBJECT_ID(N'dbo.HidrtbSensor', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.HidrtbSensorMeta', N'U') IS NOT NULL
BEGIN
    ;WITH Src AS (
        SELECT
            UPPER(LTRIM(RTRIM(s.Sens_Nombre))) AS Sens_Nombre,
            s.Cult_Id,
            s.Sens_Finca,
            s.Sens_CCEstimado,
            s.Sens_MetodoCC,
            s.Sens_FechaEstimacionCC,
            s.Sens_Activo,
            s.Sens_FechaCreacion,
            s.Sens_FechaActualizacion,
            ROW_NUMBER() OVER (
                PARTITION BY UPPER(LTRIM(RTRIM(s.Sens_Nombre)))
                ORDER BY s.Sens_Activo DESC, s.Sens_Id DESC
            ) AS rn
        FROM dbo.HidrtbSensor s
        WHERE s.Sens_Nombre IS NOT NULL
          AND LTRIM(RTRIM(s.Sens_Nombre)) <> N''
    )
    MERGE dbo.HidrtbSensorMeta AS t
    USING (
        SELECT
            Sens_Nombre,
            Cult_Id,
            Sens_Finca,
            Sens_CCEstimado,
            Sens_MetodoCC,
            Sens_FechaEstimacionCC,
            Sens_Activo,
            Sens_FechaCreacion,
            Sens_FechaActualizacion
        FROM Src
        WHERE rn = 1
    ) AS x
    ON t.Sens_Nombre = x.Sens_Nombre
    WHEN MATCHED THEN UPDATE SET
        Cult_Id = COALESCE(t.Cult_Id, x.Cult_Id),
        Sens_Finca = COALESCE(t.Sens_Finca, x.Sens_Finca),
        Sens_CCEstimado = COALESCE(t.Sens_CCEstimado, x.Sens_CCEstimado),
        Sens_MetodoCC = COALESCE(t.Sens_MetodoCC, x.Sens_MetodoCC),
        Sens_FechaEstimacionCC = COALESCE(t.Sens_FechaEstimacionCC, x.Sens_FechaEstimacionCC),
        Meta_Activo = CASE WHEN t.Meta_Activo = 1 OR x.Sens_Activo = 1 THEN 1 ELSE 0 END,
        Meta_FechaActualizacion = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT (
        Sens_Nombre, Cult_Id, Sens_Finca,
        Sens_CCEstimado, Sens_MetodoCC, Sens_FechaEstimacionCC,
        Meta_Activo, Meta_FechaCreacion, Meta_FechaActualizacion
    ) VALUES (
        x.Sens_Nombre, x.Cult_Id, x.Sens_Finca,
        x.Sens_CCEstimado, x.Sens_MetodoCC, x.Sens_FechaEstimacionCC,
        x.Sens_Activo, x.Sens_FechaCreacion, x.Sens_FechaActualizacion
    );
END
GO

/* Eliminar inventario SQL de sensores (ya migrado a Meta). */
IF OBJECT_ID(N'dbo.HidrtbSensor', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.HidrtbSensor;
END
GO
