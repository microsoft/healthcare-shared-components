// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Exceptions;

public class CompatibleVersionsNotFoundException : SqlRecordNotFoundException
{
    public CompatibleVersionsNotFoundException(string message)
        : base(message)
    {
    }
}
