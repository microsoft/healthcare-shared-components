IF EXISTS (
    SELECT *
    FROM sys.tables
    WHERE name = 'Table1')
BEGIN
    ROLLBACK TRANSACTION
    RETURN
END
