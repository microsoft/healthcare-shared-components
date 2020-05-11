// -------------------------------------------------------------------------------------------------
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
        Started,
        Completed,
        Failed,
    }
}
