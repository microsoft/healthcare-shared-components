// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Core.Features.Health;

public class HealthCheckReportEntry
{
    public string Name { get; set; }

    public string Status { get; set; }

    public string Description { get; set; }

    public IReadOnlyDictionary<string, object> Data { get; set; }
}
