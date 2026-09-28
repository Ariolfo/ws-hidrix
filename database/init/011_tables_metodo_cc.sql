/*
  Catálogo de métodos para determinar capacidad de campo (CC).
  Autor: AGROSAVIA · Hidrix | 2026-08-07
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbMetodoCC', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HidrtbMetodoCC (
        Meto_Id                 INT IDENTITY(1,1) NOT NULL,
        Meto_Nombre             NVARCHAR(120)     NOT NULL,
        Meto_Descripcion        NVARCHAR(500)     NULL,
        Meto_Activo             BIT               NOT NULL CONSTRAINT DF_HidrtbMetodoCC_Activo DEFAULT (1),
        Meto_FechaCreacion      DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbMetodoCC_Creacion DEFAULT (SYSUTCDATETIME()),
        Meto_FechaActualizacion DATETIME2(7)      NOT NULL CONSTRAINT DF_HidrtbMetodoCC_Actualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HidrtbMetodoCC PRIMARY KEY (Meto_Id),
        CONSTRAINT UQ_HidrtbMetodoCC_Nombre UNIQUE (Meto_Nombre)
    );
END
GO

MERGE dbo.HidrtbMetodoCC AS t
USING (VALUES
    (N'Método de laboratorio', N'Determinación de CC en olla de presión o mesa de tensión a 33 kPa (suelo saturado drenado).'),
    (N'Método de campo', N'Medición in situ tras riego o lluvia, cuando el drenaje libre se estabiliza (anillo o perfil).'),
    (N'Estimación por textura', N'Valores de referencia según clase textural del suelo (tablas USDA / FAO).')
) AS s (Meto_Nombre, Meto_Descripcion)
ON t.Meto_Nombre = s.Meto_Nombre
WHEN NOT MATCHED THEN
    INSERT (Meto_Nombre, Meto_Descripcion, Meto_Activo)
    VALUES (s.Meto_Nombre, s.Meto_Descripcion, 1);
GO
