// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer
{
    public static class IndentedStringBuilderExtensions
    {
        /// <summary>
        /// Appends a <see cref="Environment.NewLine"/> to the string builder if it does not already have a trailing newline.
        /// </summary>
        /// <param name="indentedStringBuilder">The string builder.</param>
        /// <returns>The same string builder.</returns>
        public static IndentedStringBuilder AppendLineIfNotConsecutive(this IndentedStringBuilder indentedStringBuilder)
        {
            var newLine = Environment.NewLine;

            if (indentedStringBuilder.Length < newLine.Length)
            {
                return indentedStringBuilder.AppendLine();
            }

            for (int i = 1; i <= newLine.Length; i++)
            {
                if (indentedStringBuilder[^i] != newLine[^i])
                {
                    return indentedStringBuilder.AppendLine();
                }
            }

            return indentedStringBuilder;
        }

        /// <summary>
        /// Helps with building a WHERE clause with 0 to many predicates ANDed together.
        /// Call <see cref="IndentedStringBuilder.DelimitedScope.BeginDelimitedElement"/> before appending
        /// a predicate and be sure to dispose the the <see cref="IndentedStringBuilder.DelimitedScope"/>
        /// at the end.
        /// </summary>
        /// <param name="indentedStringBuilder">The string builder</param>
        /// <returns>The scope</returns>
        public static IndentedStringBuilder.DelimitedScope BeginDelimitedWhereClause(this IndentedStringBuilder indentedStringBuilder)
        {
            return indentedStringBuilder.BeginDelimitedScope(
                sb =>
                {
                    sb.Append("WHERE ");
                    sb.IndentLevel++;
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.Append("AND ");
                },
                sb =>
                {
                    sb.IndentLevel--;
                    sb.AppendLine();
                });
        }

        public static IndentedStringBuilder.DelimitedScope BeginDelimitedOnClause(this IndentedStringBuilder indentedStringBuilder)
        {
            return indentedStringBuilder.BeginDelimitedScope(
                sb =>
                {
                    sb.Append("ON ");
                    sb.IndentLevel++;
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.Append("AND ");
                },
                sb =>
                {
                    sb.IndentLevel--;
                    sb.AppendLine();
                });
        }

        public static IndentedStringBuilder.DelimitedScope BeginDelimitedClause(this IndentedStringBuilder indentedStringBuilder, string delimiter)
        {
            return indentedStringBuilder.BeginDelimitedScope(
                sb =>
                {
                    sb.AppendLineIfNotConsecutive();
                    sb.AppendLine("(");
                    sb.IndentLevel++;
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.Append(delimiter);
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.IndentLevel--;
                    sb.Append(")");
                });
        }

        /// <summary>
        /// Helps with building a parenthesized nested clause with 0 to many predicates ANDed together.
        /// Call <see cref="IndentedStringBuilder.DelimitedScope.BeginDelimitedElement"/> before appending
        /// a predicate and be sure to dispose the the <see cref="IndentedStringBuilder.DelimitedScope"/>
        /// at the end.
        /// </summary>
        /// <param name="indentedStringBuilder">The string builder</param>
        /// <returns>The scope</returns>
        public static IndentedStringBuilder.DelimitedScope BeginAndedDelimitedScope(this IndentedStringBuilder indentedStringBuilder)
        {
            return indentedStringBuilder.BeginDelimitedScope(
                sb =>
                {
                    sb.AppendLineIfNotConsecutive();
                    sb.AppendLine("(");
                    sb.IndentLevel++;
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.Append("AND ");
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.IndentLevel--;
                    sb.Append(")");
                });
        }

        /// <summary>
        /// Helps with building a parenthesized nested clause with 0 to many predicates jORed together.
        /// Call <see cref="IndentedStringBuilder.DelimitedScope.BeginDelimitedElement"/> before appending
        /// a predicate and be sure to dispose the the <see cref="IndentedStringBuilder.DelimitedScope"/>
        /// at the end.
        /// </summary>
        /// <param name="indentedStringBuilder">The string builder</param>
        /// <returns>The scope</returns>
        public static IndentedStringBuilder.DelimitedScope BeginOredDelimitedScope(this IndentedStringBuilder indentedStringBuilder)
        {
            return indentedStringBuilder.BeginDelimitedScope(
                sb =>
                {
                    sb.AppendLineIfNotConsecutive();
                    sb.AppendLine("(");
                    sb.IndentLevel++;
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.Append("OR ");
                },
                sb =>
                {
                    sb.AppendLine();
                    sb.IndentLevel--;
                    sb.Append(")");
                });
        }
    }
}
