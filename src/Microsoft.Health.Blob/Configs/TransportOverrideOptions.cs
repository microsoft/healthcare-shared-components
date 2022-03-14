// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using Azure.Core.Pipeline;

namespace Microsoft.Health.Blob.Configs;

/// <summary>
/// Represents a collection of settings used to configure the underlying HTTP transport.
/// </summary>
public class TransportOverrideOptions
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Used internally to create singleton client.")]
    internal HttpPipelineTransport Transport
    {
        // Singleton, called only during app initialization, and should be available during the lifetime of the app
        // Creating our own HttpClient to handle connection timeout. We are seeing intermittent socketexceptions in
        // QIDO where we can send up to 200 concurrent Azure blob requests. This waits for 22 seconds. #85137
        // Azure SDK code that does this https://github.com/Azure/azure-sdk-for-net/blob/99f5a87233b397fb9992031300cd61fe2c4baa5c/sdk/core/Azure.Core/src/Pipeline/HttpClientTransport.cs#L143-L144
        get => new HttpClientTransport(new SocketsHttpHandler { ConnectTimeout = ConnectTimeout });
    }

    /// <inheritdoc cref="SocketsHttpHandler.ConnectTimeout"/>
    public TimeSpan ConnectTimeout { get; set; } = Timeout.InfiniteTimeSpan;
}
