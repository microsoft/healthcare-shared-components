// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

/// <summary>
/// Represents a stored procedure
/// </summary>
public class StoredProcedure
{
    public StoredProcedure(string procedureName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(procedureName, nameof(procedureName));

        ProcedureName = procedureName;
    }

    public string ProcedureName { get; }

    /// <summary>
    /// Gets the name of the stored procedure qualified by the given database.
    /// </summary>
    /// <param name="databaseName">
    /// The raw (unescaped) name of the database containing the stored procedure. The name is delimited automatically,
    /// such that the resulting name is of the form <c>[database].dbo.MyProcedure</c>.
    /// </param>
    /// <returns>The database-qualified name of the stored procedure.</returns>
    protected string GetQualifiedName(string databaseName)
    {
        EnsureArg.IsNotNullOrWhiteSpace(databaseName, nameof(databaseName));
        return $"{Identifier.Delimit(databaseName)}.{ProcedureName}";
    }

    public static implicit operator string(StoredProcedure p)
    {
        EnsureArg.IsNotNull(p, nameof(p));
        return p.ToString();
    }

    public override string ToString() => ProcedureName;
}
