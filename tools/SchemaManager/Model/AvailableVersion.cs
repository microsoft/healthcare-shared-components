// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Newtonsoft.Json;

namespace SchemaManager.Model
{
    public class AvailableVersion
    {
        public AvailableVersion(int id, [JsonProperty("script")] string scriptUri, [JsonProperty("diff")] string diffUri)
        {
            EnsureArg.IsNotNull(scriptUri, nameof(scriptUri));

            Id = id;
            ScriptUri = scriptUri;
            DiffUri = diffUri;
        }

        public int Id { get; }

        // When diffUri is null, then it has to render "N/A" on console.
#pragma warning disable CA1056 // Uri properties should not be strings
        public string ScriptUri { get; }

        public string DiffUri { get; }
#pragma warning restore CA1056 // Uri properties should not be strings
    }
}
