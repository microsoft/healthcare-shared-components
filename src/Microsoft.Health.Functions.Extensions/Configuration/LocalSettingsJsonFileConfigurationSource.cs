// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using EnsureThat;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Functions.Extensions.Configuration;

internal sealed class LocalSettingsJsonFileConfigurationSource : IConfigurationSource
{
    public readonly string _scriptPath;

    public LocalSettingsJsonFileConfigurationSource(string scriptPath)
        => _scriptPath = EnsureArg.IsNotNull(scriptPath, nameof(scriptPath));

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new LocalSettingsJsonFileConfigurationProvider(_scriptPath);

    internal sealed class LocalSettingsJsonFileConfigurationProvider : ConfigurationProvider
    {
        private const string LocalSettingsFileName = "local.settings.json";

        public readonly string _scriptPath;

        public LocalSettingsJsonFileConfigurationProvider(string scriptPath)
            => _scriptPath = EnsureArg.IsNotNull(scriptPath, nameof(scriptPath));

        public override void Load()
        {
            string path = Path.Combine(_scriptPath, LocalSettingsFileName);
            if (File.Exists(path))
            {
                using FileStream file = File.OpenRead(path);
                Settings? settings = JsonSerializer.Deserialize<Settings>(file);
                if (settings?.Values is not null)
                {
                    if (settings.Encrypted)
                    {
                        throw new InvalidOperationException($"Cannot process encrypted settings at '{path}'.");
                    }

                    foreach (KeyValuePair<string, string> entry in settings.Values)
                    {
                        Data[entry.Key.Replace("__", ":", StringComparison.Ordinal)] = entry.Value;
                    }
                }
            }
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Class is used to deserialize.")]
        private sealed class Settings
        {
            public bool Encrypted { get; init; }

            public Dictionary<string, string>? Values { get; init; }
        }
    }
}
