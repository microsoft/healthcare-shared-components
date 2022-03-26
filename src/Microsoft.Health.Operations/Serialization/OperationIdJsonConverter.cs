// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnsureThat;

namespace Microsoft.Health.Operations.Serialization;

/// <summary>
/// Represents a strict <see cref="JsonConverter{T}"/> for operation IDs.
/// </summary>
/// <remarks>
/// Unlike the typical <see cref="Guid"/> deserialization, the <see cref="OperationIdJsonConverter"/>
/// only accepts strings that can be parsed using the <see cref="OperationId.FormatSpecifier"/>.
/// </remarks>
public sealed class OperationIdJsonConverter : JsonConverter<Guid>
{
    /// <summary>
    /// Reads and converts the operation ID JSON string to its <see cref="Guid"/> representation.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> whose current token is of type <see cref="JsonTokenType.String"/>.</param>
    /// <param name="typeToConvert">The type of convert. Unused by the <see cref="OperationIdJsonConverter"/>.</param>
    /// <param name="options">A collection of options that specify how serialization should be performed.</param>
    /// <returns>The operation ID represented by the JSON string.</returns>
    /// <exception cref="JsonException">The current token cannot be read as an operation ID.</exception>
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType is JsonTokenType.String && Guid.TryParseExact(reader.GetString(), OperationId.FormatSpecifier, out Guid value)
            ? value
            : throw new JsonException();

    /// <summary>
    /// Writes a specified operation ID as a JSON string.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> with which to write.</param>
    /// <param name="value">The operation ID.</param>
    /// <param name="options">A collection of options that specify how serialization should be performed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        => EnsureArg.IsNotNull(writer, nameof(writer)).WriteStringValue(value.ToString(OperationId.FormatSpecifier));
}
