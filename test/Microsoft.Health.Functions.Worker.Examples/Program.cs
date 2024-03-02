// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Health.Functions.Worker.Examples;

IHost host = new ExampleHostBuilder().Build();
host.Run();
