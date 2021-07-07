// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Registration
{
    internal class ConfigureBlobClientFromConfigurationOptions : ConfigureFromConfigurationOptions<BlobDataStoreConfiguration>
    {
        public ConfigureBlobClientFromConfigurationOptions(IConfiguration config)
            : base(config.GetSection(BlobDataStoreConfiguration.SectionName))
        {
        }
    }
}
