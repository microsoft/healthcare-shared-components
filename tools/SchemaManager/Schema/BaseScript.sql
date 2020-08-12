-- NOTE: This script is to apply base schema required for the Schema migration tool
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md

/*************************************************************
    Schema Version
**************************************************************/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'SchemaVersion' and type = 'U')
    CREATE TABLE dbo.SchemaVersion
    (
        Version int PRIMARY KEY,
        Status varchar(10)
    )
GO

--
--  STORED PROCEDURE
--      SelectCurrentSchemaVersion
--
--  DESCRIPTION
--      Selects the current completed schema version
--
--  RETURNS
--      The current version as a result set
--

IF EXISTS (SELECT * FROM sys.objects WHERE NAME='SelectCurrentSchemaVersion' and type = 'P')
	DROP PROCEDURE SelectCurrentSchemaVersion
GO

CREATE PROCEDURE dbo.SelectCurrentSchemaVersion
AS
BEGIN
	SET NOCOUNT ON

	SELECT MAX(Version)
	FROM SchemaVersion
	WHERE Status = 'complete' OR Status = 'completed'
END
GO

--
--  STORED PROCEDURE
--      UpsertSchemaVersion
--
--  DESCRIPTION
--      Creates or updates a new schema version entry
--
--  PARAMETERS
--      @version
--          * The version number
--      @status
--          * The status of the version
--
IF EXISTS (SELECT * FROM sys.objects WHERE NAME='UpsertSchemaVersion' and type = 'P')
	DROP PROCEDURE UpsertSchemaVersion
GO

CREATE PROCEDURE dbo.UpsertSchemaVersion
    @version int,
    @status varchar(10)
AS
    SET NOCOUNT ON

    IF EXISTS(SELECT *
        FROM dbo.SchemaVersion
        WHERE Version = @version)
    BEGIN
        UPDATE dbo.SchemaVersion
        SET Status = @status
        WHERE Version = @version
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SchemaVersion
            (Version, Status)
        VALUES
            (@version, @status)
    END
GO

/*************************************************************
    Model Tables
**************************************************************/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'SearchParam' and type = 'U')
BEGIN
    CREATE TABLE dbo.SearchParam
    (
        SearchParamId smallint IDENTITY(1,1) NOT NULL,
        Uri varchar(128) COLLATE Latin1_General_100_CS_AS NOT NULL
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_SearchParam ON dbo.SearchParam
    (
        Uri
    )
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'ResourceType' and type = 'U')
BEGIN
    CREATE TABLE dbo.ResourceType
    (
        ResourceTypeId smallint IDENTITY(1,1) NOT NULL,
        Name nvarchar(50) COLLATE Latin1_General_100_CS_AS  NOT NULL
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_ResourceType on dbo.ResourceType
    (
        Name
    )
END
GO

/*************************************************************
    Capture claims on write
**************************************************************/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'ClaimType' and type = 'U')
BEGIN
    CREATE TABLE dbo.ClaimType
    (
        ClaimTypeId tinyint IDENTITY(1,1) NOT NULL,
        Name varchar(128) COLLATE Latin1_General_100_CS_AS NOT NULL
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_Claim on dbo.ClaimType
    (
        Name
    )
END
GO

/*************************************************************
    Compartments
**************************************************************/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'CompartmentType' and type = 'U')
BEGIN
    CREATE TABLE dbo.CompartmentType
    (
        CompartmentTypeId tinyint IDENTITY(1,1) NOT NULL,
        Name varchar(128) COLLATE Latin1_General_100_CS_AS NOT NULL
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_CompartmentType on dbo.CompartmentType
    (
        Name
    )
END
GO

/*************************************************************
    Create System and QuantityCode tables
**************************************************************/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'System' and type = 'U')
BEGIN
    CREATE TABLE dbo.System
    (
        SystemId int IDENTITY(1,1) NOT NULL,
        Value nvarchar(256) NOT NULL,
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_System ON dbo.System
    (
        Value
    )
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'QuantityCode' and type = 'U')
BEGIN
    CREATE TABLE dbo.QuantityCode
    (
        QuantityCodeId int IDENTITY(1,1) NOT NULL,
        Value nvarchar(256) COLLATE Latin1_General_100_CS_AS NOT NULL
    )

    CREATE UNIQUE CLUSTERED INDEX IXC_QuantityCode on dbo.QuantityCode
    (
        Value
    )
END
GO

/*************************************************************
    Instance Schema
**************************************************************/
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'InstanceSchema' and type = 'U')
BEGIN
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

    CREATE NONCLUSTERED INDEX IX_InstanceSchema_Timeout ON dbo.InstanceSchema
    (
        Timeout
    )
END
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
IF EXISTS (SELECT * FROM sys.objects WHERE NAME='GetInstanceSchemaByName' and type = 'P')
	DROP PROCEDURE GetInstanceSchemaByName
GO

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
IF EXISTS (SELECT * FROM sys.objects WHERE NAME='UpsertInstanceSchema' and type = 'P')
	DROP PROCEDURE UpsertInstanceSchema
GO

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
                                  WHERE  Status = 'completed' OR Status = 'complete' AND Version <= @maxVersion)
    IF EXISTS(SELECT * 
        FROM dbo.InstanceSchema
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
IF EXISTS (SELECT * FROM sys.objects WHERE NAME='DeleteInstanceSchema' and type = 'P')
	DROP PROCEDURE DeleteInstanceSchema
GO

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
IF EXISTS (SELECT * FROM sys.objects WHERE NAME='SelectCompatibleSchemaVersions' and type = 'P')
	DROP PROCEDURE SelectCompatibleSchemaVersions
GO

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
IF EXISTS (SELECT * FROM sys.objects WHERE NAME='SelectCurrentVersionsInformation' and type = 'P')
	DROP PROCEDURE SelectCurrentVersionsInformation
GO

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