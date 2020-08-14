// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;
using SchemaManager.Exceptions;

namespace SchemaManager
{
    internal class BaseScriptProvider : IBaseScriptProvider
    {
        public string GetCommonScript()
        {
            string resourceName = $"{typeof(BaseScriptProvider).Namespace}.Schema.BaseScript.sql";

            return Script(resourceName, string.Empty);
        }

        public string GetServiceScript(string serviceName)
        {
            string resourceName = $"{typeof(BaseScriptProvider).Namespace}.Schema.{serviceName}BaseScript.sql";

            return Script(resourceName, serviceName);
        }

        private string Script(string resourceName, string serviceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new SchemaManagerException(string.Format(Resources.BaseScriptNotFound, serviceName));
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
