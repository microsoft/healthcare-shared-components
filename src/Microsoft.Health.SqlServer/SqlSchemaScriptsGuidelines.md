# Introduction
SQLServer schema scripts are the scripts which performs schema initialization or migration for SQLServer database. These scripts are shipped along with the service binaries. Initialization scripts are 1.sql, 2.sql and so on, whereas migration scripts are 2.diff.sql, 3.diff.sql and so on.

Since execution of the migration scripts are not wrapped in the transaction explicitly via schema migration tool, so we need to be extra careful while writing the SQLServer schema migration scripts.

# Guidelines to create SQLServer scripts

## Schema Migration

### Transactional batches

1. Identify how many transactional batches are required based on the quick vs slow statements.
    - ALTER/CREATE TABLE are considerably quicker than the CREATE INDEX followed by ALTER TABLE statement or UPDATE statements.
    - We should have one transaction for quick statements.
    - We can have one or more transactaions for slower statements.
2. Add a transaction around the statements which are relatively quicker like ALTER/CREATE TABLE

        SET XACT_ABORT ON
        BEGIN TRANSACTION
        ALTER/CREATE TABLE ...
        END TRANSACTION

3. We can add one or more transactions for slower statements to get reduced recovery points.
        
        SET XACT_ABORT ON
        BEGIN TRANSACTION
        CREATE INDEX ...
        CREATE INDEX ...
        END TRANSACTION

        SET XACT_ABORT ON
        BEGIN TRANSACTION
        UPDATE TABLE ...
        END TRANSACTION

4. Also, if needed multiple internal transactions can be added if particular updates either must succeed or fail.
For ex. Add new non-nullable column and then Insert default value to non-nullable column should be in a transaction.

4. All the transactional batches must be idempotent to be able to re-execute the schema if failed halfway through.

### Idempotent statements
It is recommended to add If Not Exists or appropriate idempotent check for SQL objects.

-  CREATE TYPE
                
        IF TYPE_ID(N'Type_1') IS NULL
        BEGIN
        CREATE TYPE ...
        END

- CREATE TABLE

        IF NOT EXISTS (
             SELECT * 
             FROM sys.tables
             WHERE name = 'Dummy')
        BEGIN
        CREATE TABLE ...
        END

- CREATE INDEX

        IF NOT EXISTS (
             SELECT * 
             FROM sys.indexes
             WHERE name = 'IXC_Dummy'AND object_id = OBJECT_ID('dbo.Dummy')))
        BEGIN
        CREATE UNIQUE CLUSTERED INDEX ...
        END

- CREATE SEQUENCE

        IF NOT EXISTS (
             SELECT * 
             FROM sys.sequences
             WHERE name = 'DummySequence')
        BEGIN
        CREATE SEQUENCE ...
        END

- CREATE FULLTEXT INDEX

        IF NOT EXISTS (
             SELECT * 
             FROM sys.fulltext_indexes
             where object_id = object_id('dbo.Dummy'))
        BEGIN
        CREATE FULLTEXT INDEX ON ...
        END

- CREATE STORED PROC

        CREATE OR ALTER PROCEDURE dbo.UpsertResource
        ...


### Alter Stored Procedure in later schema version

Generally, it is advised not to alter the SPs(Stored procedures) in the later schema version. Rather create SPs with the name `<old SP Name>_<incrementingNumber>` (i.e. UpsertResource_1) for the backward compatibility
     
- Let's say current schema version is x. Due to any reason, if the schema migration failed halfway through after the SP is altered, the fhir-server would use the altered SP while being on schema version 'x'. So we want to avoid that by creating new SP and this new SP would be referred in the code if the schema version >= x+1.

## Add columns to existing tables
If we need to add columns to the existing tables, then wherever possible, we should add nullable column to the existing table for the backward compatibility. If for any specific scenario, the new column can't be nullable then we should add default value to the existing rows.

## Schema Initialization

1. If there is no [non-transactional script](https://docs.microsoft.com/en-us/previous-versions/sql/sql-server-2008-r2/ms191544(v=sql.105)?redirectedfrom=MSDN) in the x.sql(e.g. FHIR initialization script) then we should wrap the whole content of schema script in a transaction.

2. If there is any non-transactional script like CREATE FULLTEXT INDEX(e.g. DICOM initialization script), then we should make the non-transactional script idempotent and wrap the rest of the script content in a single transaction.
