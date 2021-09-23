// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Api.Extensions
{
    public static class HeaderExtensions
    {
        public static int GetTotalHeaderLength(this IHeaderDictionary headers, Encoding headerEncoding)
        {
            // Per https://en.wikipedia.org/wiki/List_of_HTTP_header_fields, each header will be of the form
            // headerKey: headerValues, and terminated by an end-of-line character sequence. The list of headers
            // will be terminated by another end-of-line character sequence.
            EnsureArg.IsNotNull(headers, nameof(headers));
            EnsureArg.IsNotNull(headerEncoding, nameof(headerEncoding));

            int headerDelimiterByteCount = headerEncoding.GetByteCount(": ");
            int headerEndOfLineCharactersByteCount = headerEncoding.GetByteCount("\r\n");
            int headerLength = 0;
            foreach (KeyValuePair<string, StringValues> header in headers)
            {
                headerLength += headerEncoding.GetByteCount(header.Key)
                    + headerDelimiterByteCount
                    + headerEncoding.GetByteCount(header.Value.ToString())
                    + headerEndOfLineCharactersByteCount;
            }

            headerLength += headerEndOfLineCharactersByteCount;

            return headerLength;
        }
    }
}
