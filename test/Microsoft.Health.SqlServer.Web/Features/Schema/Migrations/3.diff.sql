CREATE TYPE dbo.NameType_2 AS TABLE
(
    Given nvarchar(128) NOT NULL,
    Family nvarchar(128) NOT NULL
)

GO

CREATE PROCEDURE dbo.MyProcedure_2
    @names dbo.NameType_1 READONLY
AS
    SELECT 1
GO