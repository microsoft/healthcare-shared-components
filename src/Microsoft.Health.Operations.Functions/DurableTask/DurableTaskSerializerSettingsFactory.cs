// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <summary>
/// Represents a <see cref="IMessageSerializerSettingsFactory"/> whose serialization settings match those
/// found in the Durable Task Framework (DTFx).
/// </summary>
/// <remarks>
/// These settings do not serialize type information, making them more flexible than the default settings
/// for the Azure Durable Function extension.
/// </remarks>
public sealed class DurableTaskSerializerSettingsFactory : IMessageSerializerSettingsFactory
{
    /// <summary>
    /// Gets the default <see cref="JsonSerializerSettings"/> used by the Durable Task Framework (DTFx).
    /// </summary>
    /// <returns>The corresponding <see cref="JsonSerializerSettings"/> object.</returns>
    public JsonSerializerSettings CreateJsonSerializerSettings()
    {
        // Based on the framework settings:
        // https://github.com/Azure/azure-functions-durable-extension/blob/v2.6.0/src/WebJobs.Extensions.DurableTask/MessageSerializerSettingsFactory.cs
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
