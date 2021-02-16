// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Newtonsoft.Json;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager.Model
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "The Uri are written to console in string format")]
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

        public string ScriptUri { get; }

        public string DiffUri { get; }
    }
}
