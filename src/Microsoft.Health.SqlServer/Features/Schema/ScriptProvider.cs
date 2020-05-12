// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class ScriptProvider<TSchemaVersionEnum> : IScriptProvider
        where TSchemaVersionEnum : Enum
    {
        public string GetMigrationScript(int version, bool applyFullSchemaSnapshot)
        {
            string folder = $"{typeof(TSchemaVersionEnum).Namespace}.Migrations";
            string resourceName = applyFullSchemaSnapshot ? $"{folder}.{version}.sql" : $"{folder}.{version}.diff.sql";

            using (Stream stream = Assembly.GetAssembly(typeof(TSchemaVersionEnum)).GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(Resources.ScriptNotFound);
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public async Task<byte[]> GetSnapshotScriptAsBytesAsync(int version, CancellationToken cancellationToken = default)
        {
            string resourceName = $"{typeof(TSchemaVersionEnum).Namespace}.Migrations.{version}.sql";

            return await ScriptAsBytesAsync(resourceName, cancellationToken);
        }

        public async Task<byte[]> GetDiffScriptAsBytesAsync(int version, CancellationToken cancellationToken = default)
        {
            string resourceName = $"{typeof(TSchemaVersionEnum).Namespace}.Migrations.{version}.diff.sql";

            return await ScriptAsBytesAsync(resourceName, cancellationToken);
        }

        private async Task<byte[]> ScriptAsBytesAsync(string resourceName, CancellationToken cancellationToken)
        {
            using (Stream fileStream = Assembly.GetAssembly(typeof(TSchemaVersionEnum)).GetManifestResourceStream(resourceName))
            {
                if (fileStream == null)
                {
                    throw new FileNotFoundException(Resources.ScriptNotFound);
                }

                var scriptBytes = new byte[fileStream.Length];
                await fileStream.ReadAsync(scriptBytes, 0, scriptBytes.Length, cancellationToken);
                return scriptBytes;
            }
        }
    }
}
