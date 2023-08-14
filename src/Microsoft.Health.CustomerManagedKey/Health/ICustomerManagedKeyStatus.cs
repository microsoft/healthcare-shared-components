// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.CustomerManagedKey.Health;

public interface ICustomerManagedKeyStatus
{
    public IExternalResourceHealth ExternalResourceHealth { get; set; }
}
