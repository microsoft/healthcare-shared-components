-- NOTE: This script is for reference and codegen only. In order to run the schema management, schema used by the service must have this in it.
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md

/*************************************************************
    Schema Version
**************************************************************/
CREATE TABLE dbo.SchemaVersion
(
    Version int PRIMARY KEY,
    Status varchar(10)
)

INSERT INTO dbo.SchemaVersion
VALUES
    (1, 'started')

GO

/*************************************************************
    Instance Schema
**************************************************************/
CREATE TABLE dbo.InstanceSchema
(
    Name varchar(64) COLLATE Latin1_General_100_CS_AS NOT NULL,
    CurrentVersion int NOT NULL,
    MaxVersion int NOT NULL,
    MinVersion int NOT NULL,
    Timeout datetime2(0) NOT NULL
)

CREATE UNIQUE CLUSTERED INDEX IXC_InstanceSchema ON dbo.InstanceSchema
(
    Name
)

GO

--
-- STORED PROCEDURE
--     Gets schema information given its instance name.
--
-- DESCRIPTION
--     Retrieves the instance schema record from the InstanceSchema table that has the matching name.
--
-- PARAMETERS
--     @name
--         * The unique name for a particular instance
--
-- RETURN VALUE
--     The matching record.
--
CREATE PROCEDURE dbo.GetInstanceSchemaByName
    @name varchar(64)
AS
    SET NOCOUNT ON

    SELECT CurrentVersion, MaxVersion, MinVersion, Timeout
    FROM dbo.InstanceSchema
    WHERE Name = @name
GO

--
-- STORED PROCEDURE
--     Update an instance schema.
--
-- DESCRIPTION
--     Modifies an existing record in the InstanceSchema table.
--
-- PARAMETERS
--    @name
--         * The unique name for a particular instance
--     @maxVersion
--         * The maximum supported schema version for the given instance
--     @minVersion
--         * The minimum supported schema version for the given instance
--     @addMinutesOnTimeout
--         * The minutes to add
--
CREATE PROCEDURE dbo.UpsertInstanceSchema
    @name varchar(64),
    @maxVersion int,
    @minVersion int,
    @addMinutesOnTimeout int
    
AS
    SET NOCOUNT ON

    DECLARE @timeout datetime2(0) = DATEADD(minute, @addMinutesOnTimeout, SYSUTCDATETIME())
    DECLARE @currentVersion int = (SELECT COALESCE(MAX(Version), 0)
                                  FROM dbo.SchemaVersion
                                  WHERE  Status = 'completed' AND Version <= @maxVersion)
    IF EXISTS(SELECT * FROM dbo.InstanceSchema
             WHERE Name = @name)
    BEGIN
        UPDATE dbo.InstanceSchema
        SET CurrentVersion = @currentVersion, MaxVersion = @maxVersion, Timeout = @timeout
        WHERE Name = @name
        
        SELECT @currentVersion
    END
    ELSE
    BEGIN
        INSERT INTO dbo.InstanceSchema
            (Name, CurrentVersion, MaxVersion, MinVersion, Timeout)
        VALUES
            (@name, @currentVersion, @maxVersion, @minVersion, @timeout)

        SELECT @currentVersion
    END
GO

--
-- STORED PROCEDURE
--     Delete instance schema information.
--
-- DESCRIPTION
--     Delete all the expired records in the InstanceSchema table.
--
CREATE PROCEDURE dbo.DeleteInstanceSchema
    
AS
    SET NOCOUNT ON

    DELETE FROM dbo.InstanceSchema
    WHERE Timeout < SYSUTCDATETIME()

GO

--
--  STORED PROCEDURE
--      SelectCompatibleSchemaVersions
--
--  DESCRIPTION
--      Selects the compatible schema versions
--
--  RETURNS
--      The maximum and minimum compatible versions
--
CREATE PROCEDURE dbo.SelectCompatibleSchemaVersions

AS
BEGIN
    SET NOCOUNT ON

    SELECT MAX(MinVersion), MIN(MaxVersion)
    FROM dbo.InstanceSchema
    WHERE Timeout > SYSUTCDATETIME()
END
GO

--
--  STORED PROCEDURE
--      SelectCurrentVersionsInformation
--
--  DESCRIPTION
--      Selects the current schema versions information
--
--  RETURNS
--      The current versions, status and server names using that version
--
CREATE PROCEDURE dbo.SelectCurrentVersionsInformation


AS
BEGIN
    SET NOCOUNT ON

    SELECT SV.Version, SV.Status, STRING_AGG(SCH.NAME, ',')
    FROM dbo.SchemaVersion AS SV LEFT OUTER JOIN dbo.InstanceSchema AS SCH
    ON SV.Version = SCH.CurrentVersion
    GROUP BY Version, Status

END
GO

