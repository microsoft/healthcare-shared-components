// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Operations;

/// <summary>
/// A <see langword="static"/> class for utilities related to operation IDs.
/// </summary>
public static class OperationId
{
    /// <summary>
    /// Gets the <see cref="Guid"/> format specifier for normalizing operation ID string values.
    /// </summary>
    public const string FormatSpecifier = "N";

    /// <summary>
    /// Creates a new pseudo-random operation ID.
    /// </summary>
    /// <returns>A new operation ID.</returns>
    public static string Generate()
        => Guid.NewGuid().ToString(FormatSpecifier);

    /// <summary>
    /// Parses the given value as an operation ID.
    /// </summary>
    /// <param name="input">The operation ID to parse.</param>
    /// <returns>The parsed operation ID.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The value cannot be parsed as an operation ID.</exception>
    public static Guid ParseExact(string input)
        => Guid.ParseExact(input, FormatSpecifier);

    /// <summary>
    /// Attempts to parse the given value as an operation ID.
    /// </summary>
    /// <param name="input">The operation ID to parse.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed value. If the method returns <see langword="true"/>,
    /// <paramref name="result"/> contains a valid operation ID. If the method returns <see langword="false"/>,
    /// <paramref name="result"/> equals <see cref="Guid.Empty"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="input"/> represented an operation ID;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryParseExact(string input, out Guid result)
        => Guid.TryParseExact(input, FormatSpecifier, out result);
}
