// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Microsoft.Health.SqlServer
{
    // The logic for determining valid identifiers comes from the MS docs here:
    // https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15
    internal static class Identifier
    {
        private static readonly Regex NameRegex = new Regex(
            @"^[\p{L}#_]+[\p{L}\p{Nd}@$#_]*$",
            RegexOptions.Compiled);

        // Any closing bracket must be escaped
        private static readonly Regex EscapedBracketRegex = new Regex(
            @"^\[(?:[^\]]|(?:\]\]))+\]$",
            RegexOptions.Compiled);

        private static readonly Regex EscapedQuoteRegex = new Regex(
            @"^""(?:[^""]|(?:""""))+""$",
            RegexOptions.Compiled);

        // Given we use a "CREATE DATABASE" command, variables that are prefixed with "@" are disallowed
        public static bool IsValidDatabase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            else if (name[0] == '[')
            {
                return EscapedBracketRegex.IsMatch(name);
            }
            else if (name[0] == '\"')
            {
                return EscapedQuoteRegex.IsMatch(name);
            }
            else
            {
                return NameRegex.IsMatch(name) && !Keywords.SqlServer.Contains(name);
            }
        }
    }
}
