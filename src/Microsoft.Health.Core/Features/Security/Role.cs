// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using EnsureThat;

namespace Microsoft.Health.Core.Features.Security;

/// <summary>
/// Class representing the concept of a role for the RBAC System
/// </summary>
/// <typeparam name="TDataActions">Type representing the dataActions for the service</typeparam>
public class Role<TDataActions>
    where TDataActions : Enum
{
    public Role(string name, TDataActions allowedDataActions, string scope)
    {
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));
        EnsureArg.Is(scope, "/", nameof(scope));

        Name = name;
        AllowedDataActions = allowedDataActions;
        AllowedDataActionsUlong = Convert.ToUInt64(allowedDataActions, NumberFormatInfo.InvariantInfo);
        Scope = scope;
    }

    public string Name { get; }

    public TDataActions AllowedDataActions { get; }

    public ulong AllowedDataActionsUlong { get; }

    public string Scope { get; }
}
