// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;

namespace Microsoft.Health.Core.Features.Security
{
    public class Role<TEnum>
        where TEnum : Enum
    {
        public Role(string name, TEnum allowedDataActions, string scope)
        {
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));
            EnsureArg.Is(scope, "/", nameof(scope));

            Name = name;
            AllowedDataActions = allowedDataActions;
            AllowedDataActionsUlong = Convert.ToUInt64(allowedDataActions, NumberFormatInfo.InvariantInfo);
            Scope = scope;
        }

        public string Name { get; }

        public TEnum AllowedDataActions { get; }

        public ulong AllowedDataActionsUlong { get; }

        public string Scope { get; }
    }
}