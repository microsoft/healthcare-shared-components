// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Core.Features.Security.Authorization
{
    /// <summary>
    /// Service used for checking if given set of dataActions are present
    /// </summary>
    /// <typeparam name="TDataActions">Type representing the dataActions for the service</typeparam>
    public interface IAuthorizationService<TDataActions>
        where TDataActions : Enum
    {
        ValueTask<TDataActions> CheckAccess(TDataActions dataActions, CancellationToken cancellationToken);
    }
}