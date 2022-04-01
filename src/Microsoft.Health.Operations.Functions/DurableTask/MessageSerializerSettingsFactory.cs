// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

    public MessageSerializerSettingsFactory(IOptions<JsonSerializerSettings> serializerSettings)
        => _options = serializerSettings;

    /// <summary>
    /// Gets the <see cref="JsonSerializerSettings"/> as configured by the service container.
    /// </summary>
    /// <returns>The configured <see cref="JsonSerializerSettings"/> object.</returns>
    public JsonSerializerSettings CreateJsonSerializerSettings()
        => _options.Value;
}
