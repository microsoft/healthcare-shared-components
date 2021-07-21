﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SchemaVersionStatus
    {
        // Note - Updating SchemaVersionStatus to lower case to be consistent with the statuses applied when schema AutomaticUpdates is set to true

#pragma warning disable SA1300 // Element should begin with upper-case letter
        started,
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1300 // Element should begin with upper-case letter
        completed,
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1300 // Element should begin with upper-case letter
        failed,
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }
}
