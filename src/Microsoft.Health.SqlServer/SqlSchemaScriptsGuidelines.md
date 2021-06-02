# Introduction
SQLServer schema scripts are the scripts which performs schema initialization or migration for SQLServer database. These scripts are shipped along with the service binaries. Initialization scripts are 1.sql, 2.sql and so on, whereas migration scripts are 2.diff.sql, 3.diff.sql and so on.

Since execution of the migration scripts are not wrapped in the transaction, so we need to be extra careful while writing the SQLServer schema migration scripts.

# Guidelines to create SQLServer schema migration scripts

1. Add a transaction around the statements which are relatively quicker like ALTER/CREATE TABLE

        BEGIN TRANSACTION
        ALTER/CREATE TABLE ...
        END TRANSACTION

2. Do not add a transaction around the statements which are slower like CREATE INDEX or UPDATE statements. Or we can also add one or more transactions for slower statements batches for reduced recovery points.
3. All the individual scripts must be idempotent to be able to re-execute the schema if failed halfway through.
4. To remember that these multiple transactions are not required for schema initialization scripts. Since in the schema initialization scripts, we could wrap all the scripts([except non-transactional scripts](https://docs.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide?view=sql-server-ver15#starting-transactions)) in a transaction.