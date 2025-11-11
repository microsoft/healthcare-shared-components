// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Health.SqlServer.Web.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureSqlServerWebServices();

WebApplication app = builder.Build();
app.ConfigureSqlServerWebApp();

await app.RunAsync().ConfigureAwait(false);
