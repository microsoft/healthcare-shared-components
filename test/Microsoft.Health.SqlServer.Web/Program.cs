// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.SqlServer.Web;

using IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(b => b.UseStartup<Startup>())
    .Build();

await host
    .RunAsync()
    .ConfigureAwait(false);
