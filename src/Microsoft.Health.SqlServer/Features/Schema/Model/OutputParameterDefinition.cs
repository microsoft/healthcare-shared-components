// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

/// <summary>
/// Represents a output parameter definition (not the value) for a SQL stored procedure.
/// </summary>
/// <typeparam name="T">The CLR type of the parameter</typeparam>
public class OutputParameterDefinition<T> : ParameterDefinition<T>
{
    public OutputParameterDefinition(string name, SqlDbType type, bool nullable) : base(name, type, nullable)
    {
    }

    public OutputParameterDefinition(string name, SqlDbType type, bool nullable, long length)
        : base(name, type, nullable, length)
    {
    }

    public OutputParameterDefinition(string name, SqlDbType type, bool nullable, byte precision, byte scale)
        : base(name, type, nullable, precision, scale)
    {
    }

    public T GetOutputValue(SqlCommandWrapper command)
    {
        EnsureArg.IsNotNull(command, nameof(command));

        return (T)command.Parameters[Name].Value;
    }

    protected override SqlParameter CreateSqlParameter(T value)
    {
        SqlParameter result = base.CreateSqlParameter(value);
        result.Direction = ParameterDirection.Output;
        return result;
    }
}
