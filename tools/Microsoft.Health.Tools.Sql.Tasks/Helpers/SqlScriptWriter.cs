// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Health.Tools.Sql.Tasks.Helpers;

public class SqlScriptWriter : IDisposable
{
    private StreamWriter _writer;
    private readonly Sql150ScriptGenerator _sqlScriptGenerator;
    private bool _disposed;

    public SqlScriptWriter(string path)
    {
        // Ensure that the directory exist.
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        _writer = File.CreateText(path);
        _sqlScriptGenerator = new Sql150ScriptGenerator();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _writer?.Dispose();
            _writer = null;
        }

        _disposed = true;
    }

    public void WriteLine(string line)
    {
        _writer.WriteLine(line);
        _writer.Flush();
    }

    public void Write(IReadOnlyCollection<TSqlFragment> sqlObjects)
    {
#if NETFRAMEWORK
        if (sqlObjects == null)
        {
            throw new ArgumentNullException(nameof(sqlObjects));
        }
#else
        ArgumentNullException.ThrowIfNull(sqlObjects);
#endif

        foreach (var sqlObject in sqlObjects)
        {
            Write(sqlObject);
        }
    }

    public void Write(TSqlFragment sqlObject)
    {
        _sqlScriptGenerator.GenerateScript(sqlObject, _writer);
        _writer.Flush();
    }
}
