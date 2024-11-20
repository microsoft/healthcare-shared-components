// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Health.Functions.Worker.Examples;

public static class HealthCheck
{
    [Function("HealthCheck")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required for Azure Functions.")]
    public static OkResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")] HttpRequest request)
        => new OkResult();
}
