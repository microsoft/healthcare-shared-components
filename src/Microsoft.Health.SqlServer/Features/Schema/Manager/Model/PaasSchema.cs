// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager.Model
{
    public class PaasSchema
    {
        public PaasSchema(int id, string scriptContent)
        {
            EnsureArg.IsGte(id, 1, nameof(id));
            EnsureArg.IsNotNullOrWhiteSpace(scriptContent, nameof(scriptContent));

            Id = id;
            ScriptContent = scriptContent;
        }

        public int Id { get; }

        public string ScriptContent { get; }
    }
}
