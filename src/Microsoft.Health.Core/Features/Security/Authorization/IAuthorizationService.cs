// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Core.Features.Security.Authorization
{
    public interface IAuthorizationService<T>
        where T : Enum
    {
        ValueTask<T> CheckAccess(T dataActions, CancellationToken cancellationToken);
    }
}