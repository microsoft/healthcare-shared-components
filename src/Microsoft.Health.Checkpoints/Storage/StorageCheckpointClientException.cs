// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Checkpoints.Storage;

public class StorageCheckpointClientException : Exception
{
    public StorageCheckpointClientException()
    {
    }

    public StorageCheckpointClientException(string message)
        : base(message)
    {
    }

    public StorageCheckpointClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
