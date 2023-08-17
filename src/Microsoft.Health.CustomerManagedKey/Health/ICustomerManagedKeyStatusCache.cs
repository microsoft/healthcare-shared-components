// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.CustomerManagedKey.Health;

public interface ICustomerManagedKeyStatusCache
{
    Task<IExternalResourceHealth> GetCachedData();

    void SetCachedData(IExternalResourceHealth externalResourceHealth);
}
