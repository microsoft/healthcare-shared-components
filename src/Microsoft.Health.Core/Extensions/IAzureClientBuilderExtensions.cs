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
    private const string ClientIdKey = "clientId";
    private const string CredentialKey = "credential";
    private const string RetrySection = "credentialRetry";

    /// <summary>
    /// Set the credential to use for this client registration with optional retry settings.
    /// </summary>
    /// <remarks>
    /// Currently only the <c>managedidentity</c> credential type supports user-specified retry settings. Other
    /// credential types are skipped and instead rely on the underlying client factory for creation.
    /// </remarks>
    /// <typeparam name="TClient">The type of the client.</typeparam>
    /// <typeparam name="TOptions">The options type the client uses.</typeparam>
    /// <param name="builder">The client builder instance.</param>
    /// <param name="configuration">The configuration containing the settings for the credential.</param>
    /// <returns>The client builder instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="configuration"/> is <see langword="null"/>.
    /// </exception>
    public static IAzureClientBuilder<TClient, TOptions> WithRetryableCredential<TClient, TOptions>(
        this IAzureClientBuilder<TClient, TOptions> builder,
        IConfiguration configuration)
         where TOptions : class
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(configuration, nameof(configuration));

        string clientId = configuration[ClientIdKey];
        string credentialType = configuration[CredentialKey];

        // TODO: Support other credential types if necessary
        if (string.Equals(credentialType, "managedidentity", StringComparison.OrdinalIgnoreCase))
        {
            ManagedIdentityCredentialOptions options = new() { ClientId = clientId };
            configuration
                .GetSection(RetrySection)
                .Bind(options.Retry);

            ManagedIdentityCredential credential = new(options.ClientId, options);
            return builder.WithCredential(credential);
        }

        return builder;
    }

    private sealed class ManagedIdentityCredentialOptions : TokenCredentialOptions
    {
        public string ClientId { get; set; }
    }
}
