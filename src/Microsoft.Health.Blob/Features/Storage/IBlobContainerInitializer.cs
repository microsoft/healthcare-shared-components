﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace Microsoft.Health.Blob.Features.Storage
{
    public interface IBlobContainerInitializer
    {
        Task<CloudBlobContainer> InitializeContainerAsync(CloudBlobClient blobClient);
    }
}
