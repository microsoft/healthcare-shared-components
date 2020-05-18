// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace SchemaManager.Model
{
    public class AvailableVersion
    {
        public AvailableVersion(int id, Uri script, Uri diff)
        {
            EnsureArg.IsNotNull(script, nameof(script));

            Id = id;
            Script = script;
            Diff = diff;
        }

        public int Id { get; }

        public Uri Script { get; }

        public Uri Diff { get; }
    }
}
