// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Health;

public class CustomerKeyHealth
{
    public bool IsHealthy { get; set; }

    public string Description { get; set; }

    public ExternalHealthReason Reason { get; set; }

    public Exception Exception { get; set; }
}
