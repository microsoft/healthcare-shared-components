// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;

public class InstanceSchemaNotFoundException : SchemaManagerException
{
    public InstanceSchemaNotFoundException()
    {
    }

    public InstanceSchemaNotFoundException(string message)
        : base(message)
    {
    }

    public InstanceSchemaNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
