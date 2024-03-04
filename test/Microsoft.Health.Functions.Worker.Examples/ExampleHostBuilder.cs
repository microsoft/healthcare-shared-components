// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Operations.Functions.Worker;

namespace Microsoft.Health.Functions.Worker.Examples;

public class ExampleHostBuilder : WorkerHostBuilder
{
    public ExampleHostBuilder()
        : base()
    {
        HostBuilder
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureAppConfiguration(builder =>
                builder.AddJsonFile("worker.json", optional: false));
    }
}
