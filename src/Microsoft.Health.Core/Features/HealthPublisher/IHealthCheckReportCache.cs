// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.Core.Features.Health;

public interface IHealthCheckReportCache
{
    Task<HealthCheckReport> GetCachedData();

    void SetCachedData(HealthCheckReport healthCheckReport);
}
