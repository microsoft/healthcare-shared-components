// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.SqlServer.Features.Schema;

internal sealed class ConfigureSchemaInformation : IPostConfigureOptions<SqlSchemaVersionOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public ConfigureSchemaInformation(IServiceProvider serviceProvider)
        => _serviceProvider = EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

    public void PostConfigure(string name, SqlSchemaVersionOptions options)
    {
        SchemaInformation schemaInformation = _serviceProvider.GetService<SchemaInformation>();
        if (schemaInformation is not null)
        {
            options.MinimumVersion = schemaInformation.MinimumSupportedVersion;
            options.MaximumVersion = schemaInformation.MaximumSupportedVersion;
        }
    }
}
