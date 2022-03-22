// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.Functions.Extensions.DependencyInjection;

namespace Microsoft.Health.Functions.Extensions;

/// <summary>
/// A <see langword="static"/> class for utilities for interacting with the Azure Functions host.
/// </summary>
public static class AzureFunctionsJobHost
{
    /// <summary>
    /// The name of the configuration section in which all user-specified configurations reside.
    /// </summary>
    public const string RootSectionName = "AzureFunctionsJobHost";

}
