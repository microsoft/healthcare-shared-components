// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Core.Configs
{
    /// <summary>
    /// Configuration settings for authorization
    /// </summary>
    /// <typeparam name="TEnum">Type representing the dataActions for the service</typeparam>
    public class AuthorizationConfiguration<TEnum>
        where TEnum : Enum
    {
        public string RolesClaim { get; set; } = "roles";

        public bool Enabled { get; set; }

        public IReadOnlyList<Role<TEnum>> Roles { get; internal set; } = ImmutableList<Role<TEnum>>.Empty;
    }
}