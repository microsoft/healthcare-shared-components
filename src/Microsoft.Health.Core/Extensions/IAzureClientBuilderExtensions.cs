// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Core.Extensions;
using Azure.Identity;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Health.Core.Extensions;

/// <summary>
/// Provides a set of <see langword="static"/> methods for building Azure clients.
/// </summary>
public static class IAzureClientBuilderExtensions
{
    /// <summary>
    /// Set the managed identity credential to use for this client registration.
    /// </summary>
    /// <typeparam name="TClient">The type of the client.</typeparam>
    /// <typeparam name="TOptions">The options type the client uses.</typeparam>
    /// <param name="builder">The client builder instance.</param>
    /// <param name="configuration">The configuration containing the settings for the credential.</param>
    /// <returns>The client builder instance.</returns>
    /// <exception cref="InvalidOperationException">ClientId is not present in the <paramref name="configuration"/>.</exception>
    public static IAzureClientBuilder<TClient, TOptions> WithManagedIdentityCredential<TClient, TOptions>(
        this IAzureClientBuilder<TClient, TOptions> builder,
        IConfiguration configuration)
         where TOptions : class
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        ManagedIdentityCredentialOptions options = new();
        configuration.Bind(options);

        if (string.IsNullOrEmpty(options.ClientId))
            throw new InvalidOperationException("Missing ClientId for Managed Identity.");

        ManagedIdentityCredential credential = new(options.ClientId, options);

        return builder.WithCredential(credential);
    }

    private sealed class ManagedIdentityCredentialOptions : TokenCredentialOptions
    {
        public string ClientId { get; set; }
    }
}
