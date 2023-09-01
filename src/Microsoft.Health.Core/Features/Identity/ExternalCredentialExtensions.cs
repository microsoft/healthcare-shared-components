// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Health.Core.Features.Identity;

public static class ExternalCredentialExtensions
{
    public static IServiceCollection AddExternalCredentialProvider(this IServiceCollection services)
    {
        services.TryAddSingleton<IExternalCredentialProvider, DefaultExternalCredentialProvider>();
        return services;
    }
}
