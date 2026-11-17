-- Read-only inventory for a future physical column reorder. This does not rebuild tables.
-- Primary-key columns take precedence over foreign-key columns, followed by other
-- NOT NULL columns and then nullable columns. Original order is retained within each group.
SET NOCOUNT ON;

;WITH ColumnGroups AS
(
    SELECT t.object_id, s.name AS SchemaName, t.name AS TableName,
           c.column_id, c.name AS ColumnName, c.is_nullable,
           CASE
             WHEN EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic
                          ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                          WHERE i.object_id = t.object_id AND i.is_primary_key = 1 AND ic.column_id = c.column_id) THEN 0
             WHEN EXISTS (SELECT 1 FROM sys.foreign_key_columns f
                          WHERE f.parent_object_id = t.object_id AND f.parent_column_id = c.column_id) THEN 1
             WHEN c.is_nullable = 0 THEN 2 ELSE 3 END AS ColumnGroup
    FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
    JOIN sys.columns c ON c.object_id = t.object_id
    WHERE t.is_ms_shipped = 0
), Ordered AS
(
    SELECT *, ROW_NUMBER() OVER (PARTITION BY object_id ORDER BY column_id) AS CurrentPosition,
           ROW_NUMBER() OVER (PARTITION BY object_id ORDER BY ColumnGroup, column_id) AS ProposedPosition
    FROM ColumnGroups
), Affected AS
(
    SELECT DISTINCT object_id FROM Ordered WHERE CurrentPosition <> ProposedPosition
)
SELECT o.SchemaName, o.TableName, o.ColumnName, o.CurrentPosition, o.ProposedPosition,
       CASE o.ColumnGroup WHEN 0 THEN 'primary key' WHEN 1 THEN 'foreign key'
                          WHEN 2 THEN 'not null' ELSE 'nullable' END AS ColumnCategory,
       o.is_nullable
FROM Ordered o JOIN Affected a ON a.object_id = o.object_id
ORDER BY o.SchemaName, o.TableName, o.ProposedPosition;

-- Dependencies must be scripted and verified before any table is replaced.
SELECT s.name AS SchemaName, t.name AS TableName,
       (SELECT COUNT(*) FROM sys.foreign_keys f WHERE f.parent_object_id = t.object_id OR f.referenced_object_id = t.object_id) AS ForeignKeys,
       (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.index_id > 0) AS Indexes,
       (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.has_filter = 1) AS FilteredIndexes,
       (SELECT COUNT(*) FROM sys.check_constraints c WHERE c.parent_object_id = t.object_id) AS CheckConstraints,
       (SELECT COUNT(*) FROM sys.default_constraints d WHERE d.parent_object_id = t.object_id) AS DefaultConstraints,
       (SELECT COUNT(*) FROM sys.triggers tr WHERE tr.parent_id = t.object_id) AS Triggers,
       (SELECT COUNT(*) FROM sys.sql_expression_dependencies d WHERE d.referenced_id = t.object_id) AS ExpressionReferences,
       t.temporal_type_desc, t.is_memory_optimized, t.is_replicated, t.is_tracked_by_cdc
FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;
