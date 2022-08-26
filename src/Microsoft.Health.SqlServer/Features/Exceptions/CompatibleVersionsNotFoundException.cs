// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Features.Exceptions;

public class CompatibleVersionsNotFoundException : SqlRecordNotFoundException
{
    public CompatibleVersionsNotFoundException()
    {
    }

    public CompatibleVersionsNotFoundException(string message)
        : base(message)
    {
    }

    public CompatibleVersionsNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
