// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Api.Features.Cors;

namespace Microsoft.Health.Api.Configuration
{
    public interface IApiConfiguration
    {
        CorsConfiguration Cors { get; }
    }
}