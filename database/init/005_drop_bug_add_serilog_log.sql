/*
  Migra BD existente:
  - Elimina HidrtbBug (reemplazada por Serilog → HidrtbLog)
  - Crea HidrtbLog para Serilog.Sinks.MSSqlServer
*/
USE [dbHidrix];
GO

IF OBJECT_ID(N'dbo.HidrtbBug', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.HidrtbBug;
END
GO

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
