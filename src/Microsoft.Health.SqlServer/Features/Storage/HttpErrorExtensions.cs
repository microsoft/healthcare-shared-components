// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;

namespace Microsoft.Health.SqlServer.Features.Storage;

public static class HttpErrorExtensions
{
    public static bool IsInvalidAccess(this HttpRequestException exception)
    {
        return exception?.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized;
    }
}
