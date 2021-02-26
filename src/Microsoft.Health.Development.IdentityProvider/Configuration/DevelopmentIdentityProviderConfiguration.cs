// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Development.IdentityProvider.Configuration;

namespace Microsoft.Health.Development.IdentityProvider.Configuration
{
    public class DevelopmentIdentityProviderConfiguration
    {
        public const string Audience = "health-api";
        public const string LastModifiedClaim = "appid";
        public const string ClientIdClaim = "client_id";

        public bool Enabled { get; set; }

        public IList<Application> ClientApplications { get; } = new List<Application>();

        public IList<User> Users { get; } = new List<User>();
    }
}