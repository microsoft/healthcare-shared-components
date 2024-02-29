// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Functions.Extensions.Configuration;

// This class is a subset of the same class found inside of the Azure Functions Host:
// https://github.com/Azure/azure-functions-host/blob/dev/src/WebJobs.Script/Config/HostJsonFileConfigurationSource.cs
internal sealed class HostJsonFileConfigurationSource : IConfigurationSource
{
    private readonly string _scriptPath;

    public HostJsonFileConfigurationSource(string scriptPath)
        => _scriptPath = EnsureArg.IsNotNull(scriptPath, nameof(scriptPath));

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new HostJsonFileConfigurationProvider(_scriptPath);

    internal sealed class HostJsonFileConfigurationProvider : ConfigurationProvider
    {
        private const string HostMetadataFileName = "host.json";
        private const string LatestHostConfigVersion = "2.0";

        private readonly string _scriptPath;
        private readonly Stack<string> _path = new Stack<string>();

        public HostJsonFileConfigurationProvider(string scriptPath)
            => _scriptPath = EnsureArg.IsNotNull(scriptPath, nameof(scriptPath));

        public override void Load()
        {
            JObject hostJson = LoadHostConfigurationFile();
            ProcessObject(hostJson);
        }

        private void ProcessObject(JObject hostJson)
        {
            foreach (JProperty property in hostJson.Properties())
            {
                _path.Push(property.Name);
                ProcessProperty(property);
                _path.Pop();
            }
        }

        private void ProcessProperty(JProperty property)
            => ProcessToken(property.Value);

        private void ProcessToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    ProcessObject(token.Value<JObject>()!);
                    break;
                case JTokenType.Array:
                    ProcessArray(token.Value<JArray>()!);
                    break;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.TimeSpan:
                    string key = AzureFunctionsJobHost.RootSectionName + ConfigurationPath.KeyDelimiter + ConfigurationPath.Combine(_path.Reverse());
                    Data[key] = token.Value<JValue>()!.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    break;
            }
        }

        private void ProcessArray(JArray jArray)
        {
            for (int i = 0; i < jArray.Count; i++)
            {
                _path.Push(i.ToString(CultureInfo.InvariantCulture));
                ProcessToken(jArray[i]);
                _path.Pop();
            }
        }

        /// <summary>
        /// Read and apply host.json configuration.
        /// </summary>
        private JObject LoadHostConfigurationFile()
        {
            // Before configuration has been fully read, configure a default logger factory
            // to ensure we can log any configuration errors. There's no filters at this point,
            // but that's okay since we can't build filters until we apply configuration below.
            // We'll recreate the loggers after config is read. We initialize the public logger
            // to the startup logger until we've read configuration settings and can create the real logger.
            // The "startup" logger is used in this class for startup related logs. The public logger is used
            // for all other logging after startup.

            string hostFilePath = Path.Combine(_scriptPath, HostMetadataFileName);
            JObject hostConfigObject = LoadHostConfig(hostFilePath);
            hostConfigObject = InitializeHostConfig(hostFilePath, hostConfigObject);

            return hostConfigObject;
        }

        private static JObject InitializeHostConfig(string hostJsonPath, JObject hostConfigObject)
        {
            // If the object is empty, initialize it to include the version and write the file.
            if (!hostConfigObject.HasValues)
            {
                hostConfigObject = GetDefaultHostConfigObject();
                TryWriteHostJson(hostJsonPath, hostConfigObject);
            }

            string? hostJsonVersion = hostConfigObject["version"]?.Value<string>();
            if (string.IsNullOrEmpty(hostJsonVersion))
            {
                throw new FormatException($"The {HostMetadataFileName} file is missing the required 'version' property. See https://aka.ms/functions-hostjson for steps to migrate the configuration file.");
            }

            if (!hostJsonVersion.Equals(LatestHostConfigVersion, StringComparison.OrdinalIgnoreCase))
            {
                StringBuilder errorMsg = new StringBuilder($"'{hostJsonVersion}' is an invalid value for {HostMetadataFileName} 'version' property. We recommend you set the 'version' property to '2.0'. ");
                if (hostJsonVersion.StartsWith('3'))
                {
                    // In case the customer has confused host.json version with the fact that they're running Functions v3
                    errorMsg.Append($"This does not correspond to the function runtime version, only to the schema version of the {HostMetadataFileName} file. ");
                }
                errorMsg.Append($"See https://aka.ms/functions-hostjson for more information on this configuration file.");
                throw new FormatException(errorMsg.ToString());
            }

            return hostConfigObject;
        }

        internal static JObject LoadHostConfig(string configFilePath)
        {
            JObject hostConfigObject;
            if (File.Exists(configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(configFilePath);
                    hostConfigObject = JObject.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new FormatException($"Unable to parse host configuration file '{configFilePath}'.", ex);
                }
            }
            else
            {
                // There isn't a clean way to create a new function app resource with host.json as initial payload.
                // So a newly created function app from the portal would have no host.json. In that case we need to
                // create a new function app with host.json
                hostConfigObject = GetDefaultHostConfigObject();

                // Add bundle configuration if no file exists and file system is not read only
                TryWriteHostJson(configFilePath, hostConfigObject);
            }

            return hostConfigObject;
        }

        private static JObject GetDefaultHostConfigObject()
            => JObject.Parse("{'version': '2.0'}");

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Do not throw if we cannot write the file for any reason.")]
        private static void TryWriteHostJson(string filePath, JObject content)
        {
            try
            {
                File.WriteAllText(filePath, content.ToString(Formatting.Indented));
            }
            catch
            { }
        }

        private static JObject TryAddBundleConfiguration(JObject content, string bundleId, string bundleVersion)
        {
            string bundleConfiguration = "{ 'id': '" + bundleId + "', 'version': '" + bundleVersion + "'}";
            content.Add("extensionBundle", JToken.Parse(bundleConfiguration));

            return content;
        }
    }
}
