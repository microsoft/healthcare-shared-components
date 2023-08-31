// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Core.Features.Metric;

internal class DefaultResourceHealthSignalProvider : IResourceHealthSignalProvider
{
    private readonly ILogger<DefaultResourceHealthSignalProvider> _logger;

    public DefaultResourceHealthSignalProvider(ILogger<DefaultResourceHealthSignalProvider> logger)
    {
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public void EmitHealthMetric(HealthStatusReason reason, ResourceHealthDimensionOptions resourceHealthDimension)
    {
        _logger.LogInformation("No signal provider has been configured to emit resource health metric with dimensions: {ArmResourceId}, {ArmGeoLocation}, {ResourceType}, {Reason}",
            resourceHealthDimension.ArmResourceId,
            resourceHealthDimension.ArmGeoLocation,
            resourceHealthDimension.ResourceType,
            reason);
    }
}
