// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.Health.Tools.Sql.Tasks.Helpers;

public static class SqlScriptParser
{
    /// <summary>
    /// Read the Sql file and return the parsed SqlFragment
    /// </summary>
    /// <param name="sqlFile">SqlFilePath to parse</param>
    /// <param name="taskLoggingHelper">Logger</param>
    /// <returns>Sql object</returns>
    public static TSqlFragment ParseSqlFile(string sqlFile, TaskLoggingHelper taskLoggingHelper)
    {
#if NETFRAMEWORK
        if (taskLoggingHelper == null)
        {
            throw new ArgumentNullException(nameof(taskLoggingHelper));
        }
#else
        ArgumentNullException.ThrowIfNull(taskLoggingHelper);
#endif

        TSqlFragment sqlFragment = null;
        using (var stream = File.OpenRead(sqlFile))
        using (var reader = new StreamReader(stream))
        {
            var parser = new TSql150Parser(true);
            sqlFragment = parser.Parse(reader, out var errors);

            if (errors != null && errors.Any())
            {
                StringBuilder sb = new StringBuilder();
                foreach (var error in errors)
                {
#if NETFRAMEWORK
                    sb.AppendLine($"Line: {error.Line}, Number: {error.Number}, Message: {error.Message}");
#else
                    sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Line: {error.Line}, Number: {error.Number}, Message: {error.Message}");
#endif
                }

                taskLoggingHelper.LogError("Failed to parse the Sql file: {0}, Error: {1}", sqlFile, sb.ToString());
            }
        }

        return sqlFragment;
    }
}
