# Introduction
SQLServer schema scripts are the scripts which performs schema initialization or migration for SQLServer database. These scripts are shipped along with the service binaries. Initialization scripts are 1.sql, 2.sql and so on, whereas migration scripts are 2.diff.sql, 3.diff.sql and so on.

Since execution of the migration scripts are not wrapped in the transaction, so we need to be extra careful while writing the SQLServer schema migration scripts.

# Guidelines to create SQLServer schema migration scripts

1. Identify how many transactional batches are required based on the quick vs slow statements.
    - ALTER/CREATE TABLE are considerably quicker than the CREATE INDEX followed by ALTER TABLE statement or UPDATE statements.
    - We should have one transaction for quick statements.
    - We can have one or more transactaions for slower statements.
2. Add a transaction around the statements which are relatively quicker like ALTER/CREATE TABLE

        SET XACT_ABORT ON
        BEGIN TRANSACTION
        ALTER/CREATE TABLE ...
        END TRANSACTION

2. We can add one or more transactions for slower statements to get reduced recovery points.
        
        SET XACT_ABORT ON
        BEGIN TRANSACTION
        CREATE INDEX ...
        CREATE INDEX ...
        END TRANSACTION

        SET XACT_ABORT ON
        BEGIN TRANSACTION
        UPDATE TABLE ...
        END TRANSACTION

3. All the transactional batches must be idempotent to be able to re-execute the schema if failed halfway through.

4. To remember that these multiple transactions are not required for schema initialization scripts. Since in the schema initialization scripts, we could wrap all the scripts([except non-transactional scripts](https://docs.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide?view=sql-server-ver15#starting-transactions)) in a transaction.
