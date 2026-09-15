/*
  Semilla: redes–país y cultivos (calculadora).
  El inventario de sensores vive en Visualiti; cultivo/CC en HidrtbSensorMeta.
  Autor: AGROSAVIA · Hidrix | 2026-09-15
*/
USE [dbHidrix];
GO

/* Redes ↔ país */
MERGE dbo.HidrtbRed AS t
USING (VALUES
    (N'RED ASORUT', 170),
    (N'RED ECUADOR', 218),
    (N'RED HONDURA', 340)
) AS s (Red_Nombre, Pais_Id)
ON t.Red_Nombre = s.Red_Nombre
WHEN MATCHED THEN UPDATE SET Pais_Id = s.Pais_Id
WHEN NOT MATCHED THEN INSERT (Red_Nombre, Pais_Id) VALUES (s.Red_Nombre, s.Pais_Id);
GO

/* Cultivos de la calculadora */
MERGE dbo.HidrtbCultivo AS t
USING (VALUES
    (N'Aguacate', CAST(39 AS DECIMAL(8,2)), CAST(31.20 AS DECIMAL(8,2)), CAST(24.96 AS DECIMAL(8,2))),
    (N'Cacao',    CAST(34 AS DECIMAL(8,2)), CAST(27.20 AS DECIMAL(8,2)), CAST(21.76 AS DECIMAL(8,2))),
    (N'Lima',     CAST(36 AS DECIMAL(8,2)), CAST(28.80 AS DECIMAL(8,2)), CAST(23.04 AS DECIMAL(8,2))),
    (N'Papaya',   CAST(34 AS DECIMAL(8,2)), CAST(27.20 AS DECIMAL(8,2)), CAST(21.76 AS DECIMAL(8,2)))
) AS s (Cult_Nombre, Cult_CapacidadCampo, Cult_PorcentajeMaximo, Cult_DecisionRiego)
ON t.Cult_Nombre = s.Cult_Nombre
WHEN MATCHED THEN UPDATE SET
    Cult_CapacidadCampo = s.Cult_CapacidadCampo,
    Cult_PorcentajeMaximo = s.Cult_PorcentajeMaximo,
    Cult_DecisionRiego = s.Cult_DecisionRiego,
    Cult_FechaActualizacion = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Cult_Nombre, Cult_CapacidadCampo, Cult_PorcentajeMaximo, Cult_DecisionRiego)
    VALUES (s.Cult_Nombre, s.Cult_CapacidadCampo, s.Cult_PorcentajeMaximo, s.Cult_DecisionRiego);
GO
