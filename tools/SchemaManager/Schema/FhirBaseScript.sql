-- NOTE: This script is to apply base FHIR schema required to start the FHIR Server for Azure
-- Style guide: please see: https://github.com/ktaranov/sqlserver-kit/blob/master/SQL%20Server%20Name%20Convention%20and%20T-SQL%20Programming%20Style.md

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