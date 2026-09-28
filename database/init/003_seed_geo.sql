/*
  Semilla geográfica base — países ISO 3166-1 numéricos.
  Departamentos/ciudades Colombia: 004_seed_colombia_geo_full.sql (DANE).
  Ecuador/Honduras: 010_seed_ecuador_honduras.sql (GeoNames / ISO 3166-2).
  Autor: AGROSAVIA · Hidrix | 2026-08-07
*/
USE [dbHidrix];
GO

MERGE dbo.HidrtbPais AS t
USING (VALUES
    (170, N'COLOMBIA', 2),
    (218, N'ECUADOR', 2),
    (340, N'HONDURAS', 2)
) AS s (Pais_Id, Pais_Nombre, Pais_Estado)
ON t.Pais_Id = s.Pais_Id
WHEN MATCHED THEN UPDATE SET Pais_Nombre = s.Pais_Nombre, Pais_Estado = s.Pais_Estado
WHEN NOT MATCHED THEN INSERT (Pais_Id, Pais_Nombre, Pais_Estado) VALUES (s.Pais_Id, s.Pais_Nombre, s.Pais_Estado);
GO
