// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Encryption.Customer.Health;

internal interface IInaccessibleStateTestProvider
{
    Task AssertHealthAsync(CancellationToken cancellationToken = default);
}
