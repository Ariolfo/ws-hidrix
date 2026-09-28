/*
  Prepara la BD existente para ASP.NET Identity (migración EF IdentityMigration).
  - Elimina HidrtbUsuario antiguo (BIGINT) incompatible con Identity (NVARCHAR Id).
  - Elimina tablas legacy ya no usadas por el código actual.
  Los usuarios previos deben volver a registrarse (no había hash de contraseña).
  Autor: AGROSAVIA · Hidrix | 2026-08-12
*/
USE [dbHidrix];
GO

/* ---- Tablas legacy fuera del modelo actual ---- */
IF OBJECT_ID(N'dbo.HidrtbUsuarioDispositivo', N'U') IS NOT NULL
    DROP TABLE dbo.HidrtbUsuarioDispositivo;
GO

IF OBJECT_ID(N'dbo.HidrtbDevicePushToken', N'U') IS NOT NULL
    DROP TABLE dbo.HidrtbDevicePushToken;
GO

IF OBJECT_ID(N'dbo.HidrtbEventoUsuario', N'U') IS NOT NULL
    DROP TABLE dbo.HidrtbEventoUsuario;
GO

IF OBJECT_ID(N'dbo.HidrtbMetodoCC', N'U') IS NOT NULL
    DROP TABLE dbo.HidrtbMetodoCC;
GO

/* ---- Usuario antiguo (solo si aún es BIGINT / sin columnas Identity) ---- */
IF OBJECT_ID(N'dbo.HidrtbUsuario', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HidrtbUsuario', N'PasswordHash') IS NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'';

    SELECT @sql = @sql + N'ALTER TABLE dbo.HidrtbUsuario DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.HidrtbUsuario');

    SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
        + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
        + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
    FROM sys.foreign_keys fk
    WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.HidrtbUsuario');

    IF LEN(@sql) > 0
        EXEC sp_executesql @sql;

    DROP TABLE dbo.HidrtbUsuario;
END
GO

/* Identity tables will be created by: dotnet ef database update */
PRINT N'OK: BD lista para IdentityMigration.';
GO
