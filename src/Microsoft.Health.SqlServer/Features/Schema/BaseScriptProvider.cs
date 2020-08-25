// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class BaseScriptProvider : IBaseScriptProvider
    {
        public string GetScript()
        {
            string resourceName = $"{typeof(SchemaInitializer).Namespace}.Migrations.BaseSchema.sql";

            using (Stream stream = Assembly.GetAssembly(typeof(SchemaInitializer)).GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(Resources.BaseScriptNotFound);
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
