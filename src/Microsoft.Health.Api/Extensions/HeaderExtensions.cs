// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Health.Api.Extensions;

public static class HeaderExtensions
{
    private static readonly Encoding HeaderEncoding = Encoding.GetEncoding("ISO-8859-1");
    public static readonly int HeaderDelimiterByteCount = HeaderEncoding.GetByteCount(": ");
    public static readonly int HeaderEndOfLineCharactersByteCount = HeaderEncoding.GetByteCount("\r\n");

    public static int GetTotalHeaderLength(this IHeaderDictionary headers)
    {
        // Per https://en.wikipedia.org/wiki/List_of_HTTP_header_fields, each header will be of the form
        // headerKey: headerValues, and terminated by an end-of-line character sequence. The list of headers
        // will be terminated by another end-of-line character sequence.
        EnsureArg.IsNotNull(headers, nameof(headers));

        int headerLength = 0;
        foreach (KeyValuePair<string, StringValues> header in headers)
        {
            headerLength += HeaderEncoding.GetByteCount(header.Key)
                + HeaderDelimiterByteCount
                + GetByteCount(header.Value)
                + HeaderEndOfLineCharactersByteCount;
        }

        headerLength += HeaderEndOfLineCharactersByteCount;

        return headerLength;
    }

    public static int GetByteCount(StringValues values)
    {
        int totalByteCountOfValues = 0;

        foreach (var value in values)
        {
            totalByteCountOfValues += HeaderEncoding.GetByteCount(value);
        }

        return totalByteCountOfValues;
    }
}
