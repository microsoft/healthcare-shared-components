// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

/// <summary>
/// Represents a SQL table.
/// </summary>
public class Table
{
    private readonly string _qualifiedName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class.
    /// </summary>
    /// <param name="tableName">The schema-qualified name of the table. For example, <c>dbo.MyTable</c>.</param>
    public Table(string tableName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(tableName, nameof(tableName));

        TableName = tableName;
        _qualifiedName = tableName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class that is scoped to a particular database.
    /// </summary>
    /// <param name="tableName">The schema-qualified name of the table. For example, <c>dbo.MyTable</c>.</param>
    /// <param name="databaseName">
    /// The raw (unescaped) name of the database containing the table. The name is delimited automatically,
    /// such that the resulting name is of the form <c>[database].dbo.MyTable</c>.
    /// </param>
    public Table(string tableName, string databaseName)
        : this(tableName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(databaseName, nameof(databaseName));

        DatabaseName = databaseName;
        _qualifiedName = $"{Identifier.Delimit(databaseName)}.{tableName}";
    }

    /// <summary>
    /// Gets the schema-qualified name of the table.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the optional name of the database containing the table, if specified.
    /// </summary>
    /// <value>The raw database name if specified; otherwise, <see langword="null"/>.</value>
    public string DatabaseName { get; }

    public static implicit operator string(Table t)
    {
        EnsureArg.IsNotNull(t, nameof(t));
        return t.ToString();
    }

    /// <summary>
    /// Returns the name of the table, including the database if specified.
    /// </summary>
    /// <returns>The qualified name of the table.</returns>
    public override string ToString() => _qualifiedName;
}
