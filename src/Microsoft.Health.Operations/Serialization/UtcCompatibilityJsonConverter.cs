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
/// Represents a <see cref="JsonConverter{T}"/> for <see cref="DateTimeOffset"/>
/// that ensures their JSON representation is equivalent to that of <see cref="DateTime"/>
/// for UTC values.
/// </summary>
public sealed class UtcCompatibilityJsonConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// Reads the <see cref="DateTimeOffset"/> from its JSON representation.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> whose current token is of type <see cref="JsonTokenType.String"/>.</param>
    /// <param name="typeToConvert">The type of convert. Unused by the <see cref="UtcCompatibilityJsonConverter"/>.</param>
    /// <param name="options">A collection of options that specify how serialization should be performed.</param>
    /// <returns>The <see cref="DateTimeOffset"/> represented by the JSON string.</returns>
    /// <exception cref="JsonException">The current token cannot be read as a <see cref="DateTimeOffset"/>.</exception>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<DateTimeOffset>(ref reader);

    /// <summary>
    /// Writes the specified <paramref name="value"/> as a JSON string that is equivalent to <see cref="DateTime"/>
    /// if the <see cref="DateTimeOffset"/> is in UTC.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> with which to write.</param>
    /// <param name="value">The <see cref="DateTimeOffset"/>.</param>
    /// <param name="options">A collection of options that specify how serialization should be performed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        EnsureArg.IsNotNull(writer, nameof(writer));

        if (value.Offset == TimeSpan.Zero)
            JsonSerializer.Serialize(writer, value.UtcDateTime);
        else
            JsonSerializer.Serialize(writer, value);
    }
}
