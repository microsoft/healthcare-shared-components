// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <summary>
/// Represents an <see cref="IMessageSerializerSettingsFactory"/> whose serialization settings are derived
/// from configured <see cref="JsonSerializerSettings"/> options.
/// </summary>
public sealed class MessageSerializerSettingsFactory : IMessageSerializerSettingsFactory
{
    private readonly IOptions<JsonSerializerSettings> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSerializerSettingsFactory"/> class
    /// based on the given <paramref name="serializerSettings"/>.
    /// </summary>
    /// <param name="serializerSettings">The JSON.NET serialization settings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serializerSettings"/> is <see langword="null"/>.</exception>
    public MessageSerializerSettingsFactory(IOptions<JsonSerializerSettings> serializerSettings)
        => _options = EnsureArg.IsNotNull(serializerSettings, nameof(serializerSettings));

    /// <summary>
    /// Gets the <see cref="JsonSerializerSettings"/> as configured by the service container.
    /// </summary>
    /// <returns>The configured <see cref="JsonSerializerSettings"/> object.</returns>
    public JsonSerializerSettings CreateJsonSerializerSettings()
        => _options.Value;
}
