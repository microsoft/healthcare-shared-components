-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md

/*************************************************************
    Schema Version
**************************************************************/

INSERT INTO dbo.SchemaVersion
VALUES
    (3, 'started')

GO

CREATE TYPE dbo.NameType_2 AS TABLE
(
    Given nvarchar(128) NOT NULL,
    Family nvarchar(128) NOT NULL
)

CREATE TYPE dbo.ComplexNumber_1 AS TABLE
(
    A nvarchar(128) NOT NULL,
    B nvarchar(128) NOT NULL
)

GO

CREATE PROCEDURE dbo.MyProcedure_2
    @names dbo.NameType_1 READONLY
AS
    SELECT 1
GO

CREATE PROCEDURE dbo.InsertNumbers
    @names dbo.ComplexNumber_1 READONLY
AS
    SELECT 1
GO

CREATE TABLE dbo.Table1
(
    Id int,
    Name nvarchar(20)
)

CREATE TABLE dbo.Table2
(
    Id int,
    City nvarchar(20)
)

GO

CREATE VIEW dbo.MyView
	WITH SCHEMABINDING
	AS

	SELECT 
        t1.Id, 
        t1.Name, 
        t2.City AS TheCity
	FROM dbo.Table1 t1
	INNER JOIN dbo.Table2 t2 ON t1.Id = t2.Id
GO

CREATE UNIQUE CLUSTERED INDEX IXC_View12 ON dbo.MyView
(
    Id,
    Name,
    TheCity
)
WITH (DATA_COMPRESSION = PAGE)

CREATE NONCLUSTERED INDEX IX_View12_City ON dbo.MyView
(	
    TheCity
)
WITH (DATA_COMPRESSION = PAGE)
