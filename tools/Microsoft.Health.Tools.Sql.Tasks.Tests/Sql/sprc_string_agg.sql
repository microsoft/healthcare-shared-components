CREATE procedure dbo.TestSP
with execute as 'dbo'
as 
set nocount on
SELECT STRING_AGG (CONVERT(NVARCHAR(max),'abc'), CHAR(13)) WITHIN GROUP (ORDER BY key_ordinal)
FROM sys.index_columns IC

GO
