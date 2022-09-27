
/*************************************************************************************************
    Auto-Generated from Sql build task. Do not manually edit it. 
**************************************************************************************************/
SET XACT_ABORT ON
BEGIN TRAN
IF EXISTS (SELECT *
           FROM   sys.tables
           WHERE  name = 'Table1')
    BEGIN
        ROLLBACK;
        RETURN;
    END

CREATE TABLE dbo.Table1 (
    Key1 BIGINT NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

CREATE TABLE dbo.Table2 (
    Key1 BIGINT NOT NULL
)
WITH (DATA_COMPRESSION = PAGE);

COMMIT
GO
CREATE PROCEDURE dbo.TestSP
WITH EXECUTE AS 'dbo'
AS
SET NOCOUNT ON;
SELECT STRING_AGG(CONVERT (NVARCHAR (MAX), 'abc'), CHAR(13)) WITHIN GROUP (ORDER BY key_ordinal) AS KeyCols
FROM   sys.index_columns AS IC;

GO
CREATE OR ALTER PROCEDURE dbo.Output1
@Result BIGINT OUTPUT
AS
SET NOCOUNT ON;
BEGIN
    SET @Result = 1;
END

GO
CREATE OR ALTER PROCEDURE dbo.GetTable1
@Uid BIGINT
AS
SET NOCOUNT ON;
BEGIN
    SELECT *
    FROM   Table1
    WHERE  Key1 = @Uid;
END

GO
