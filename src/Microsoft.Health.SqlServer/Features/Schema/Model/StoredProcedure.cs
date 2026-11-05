// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

/// <summary>
/// Represents a stored procedure.
/// </summary>
public class StoredProcedure
{
    private readonly string _qualifiedName;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedure"/> class.
    /// </summary>
    /// <param name="procedureName">The schema-qualified name of the stored procedure. For example, <c>dbo.MyProcedure</c>.</param>
    public StoredProcedure(string procedureName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(procedureName, nameof(procedureName));

        ProcedureName = procedureName;
        _qualifiedName = procedureName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedure"/> class that is scoped to a particular database.
    /// </summary>
    /// <param name="procedureName">The schema-qualified name of the stored procedure. For example, <c>dbo.MyProcedure</c>.</param>
    /// <param name="databaseName">
    /// The raw (unescaped) name of the database containing the stored procedure. The name is delimited automatically,
    /// such that the resulting name is of the form <c>[database].dbo.MyProcedure</c>.
    /// </param>
    public StoredProcedure(string procedureName, string databaseName)
        : this(procedureName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(databaseName, nameof(databaseName));

        DatabaseName = databaseName;
        _qualifiedName = $"{Identifier.Delimit(databaseName)}.{procedureName}";
    }

    /// <summary>
    /// Gets the schema-qualified name of the stored procedure.
    /// </summary>
    public string ProcedureName { get; }

    /// <summary>
    /// Gets the optional name of the database containing the stored procedure, if specified.
    /// </summary>
    /// <value>The raw database name if specified; otherwise, <see langword="null"/>.</value>
    public string DatabaseName { get; }

    public static implicit operator string(StoredProcedure p)
    {
        EnsureArg.IsNotNull(p, nameof(p));
        return p.ToString();
    }

    /// <summary>
    /// Returns the name of the stored procedure, including the database if specified.
    /// </summary>
    /// <returns>The qualified name of the stored procedure.</returns>
    public override string ToString() => _qualifiedName;
}
