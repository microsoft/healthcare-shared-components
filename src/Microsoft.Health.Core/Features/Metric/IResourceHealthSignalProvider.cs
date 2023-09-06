// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Core.Features.Metric;

public interface IResourceHealthSignalProvider
{
    void EmitHealthMetric(HealthStatusReason reason, ResourceHealthDimensionOptions resourceHealthDimension);
}
