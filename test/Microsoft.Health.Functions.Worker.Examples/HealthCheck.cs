// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Health.Functions.Worker.Examples;

public static class HealthCheck
{
    [Function("HealthCheck")]
    public static OkResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "healthz")] HttpRequest request)
        => new OkResult();
}
