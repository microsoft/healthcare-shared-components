// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace SchemaManager.Model
{
    public class AvailableVersion
    {
        public AvailableVersion(int id, string script, string diff)
        {
            EnsureArg.IsNotNull(script, nameof(script));

            Id = id;
            Script = script;
            Diff = diff;
        }

        public int Id { get; }

        public string Script { get; }

        public string Diff { get; }
    }
}
