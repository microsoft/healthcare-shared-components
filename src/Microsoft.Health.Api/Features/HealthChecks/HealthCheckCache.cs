// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Api.Features.HealthChecks;

internal sealed class HealthCheckCache(IOptions<HealthCheckServiceOptions> serviceOptions, IOptions<HealthCheckCachingOptions> cachingOptions, ILoggerFactory loggerFactory) : IDisposable
{
    private readonly ImmutableDictionary<string, HealthCheckResultCache> _results = EnsureArg.IsNotNull(serviceOptions?.Value, nameof(serviceOptions))
        .Registrations
        .Select(x => KeyValuePair.Create(x.Name, new HealthCheckResultCache(cachingOptions, loggerFactory)))
        .ToImmutableDictionary(StringComparer.Ordinal);

    public HealthCheckResultCache GetResultCache(string name)
        => _results[name];

    public void Dispose()
    {
        foreach (HealthCheckResultCache result in _results.Values)
            result.Dispose();
    }
}
