// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Storage.Blobs;

namespace Microsoft.Health.Blob.Configs;

/// <summary>
/// Represents a collection of settings used to configure a <see cref="BlobServiceClient"/> and its operations.
/// </summary>
public class BlobServiceClientOptions : BlobClientOptions
{
    /// <summary>
    /// A default configuration section name that may be used for binding.
    /// </summary>
    public const string DefaultSectionName = "BlobStore";

    /// <summary>
    /// Gets or sets the Azure Blob Storage connection string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Connection strings may be URLs or semicolon-delimited (;) key-value pairs. When creating a
    /// <see cref="BlobServiceClient"/>, the ctor <see cref="BlobServiceClient(string)"/> only
    /// expects key-value pairs while the <see cref="System.Uri"/> overloads unsurprisingly expect URLs.
    /// </para>
    /// <para>
    /// Key-value pair connection strings describe different aspects of the connection including
    /// <c>"DefaultEndpointsProtocol"</c>, <c>"AccountName"</c>, and <c>"AccountKey"</c>. Each pair has its key
    /// and value separated by an equal sign (=) like in the following example:
    /// <c>"DefaultEndpointsProtocol=https;AccountName=myAccountName;AccountKey=myAccountKey"</c>.
    /// </para>
    /// <para>
    /// <see cref="ConnectionString"/> and <see cref="ServiceUri"/> are mutually exclusive.
    /// </para>
    /// </remarks>
    /// <value>The connection string key-value pairs.</value>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Azure Blob Storage service URI.
    /// </summary>
    /// <remarks><see cref="ConnectionString"/> and <see cref="ServiceUri"/> are mutually exclusive.</remarks>
    /// <value>The URI for the Azure Blob Storage service if specified; otherwise <see langword="null"/>.</value>
    public Uri ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the type of credential used to authenticate the client.
    /// </summary>
    /// <remarks>
    /// Currently only <c>"managedidentity"</c> or <see langword="null"/> is supported.
    /// </remarks>
    /// <value>
    /// The type of credential or <see langword="null"/> if using an
    /// account key in the <see cref="ConnectionString"/>.
    /// </value>
    public string Credential { get; set; }

    /// <summary>
    /// Gets or sets the options for <see cref="BlobServiceClient"/> operations.
    /// </summary>
    /// <value>The operation settings.</value>
    public BlobOperationOptions Operations { get; set; }

    /// <summary>
    /// Gets or sets the options for configuring the <see cref="Azure.Core.ClientOptions.Transport"/>
    /// via a collection of settings.
    /// </summary>
    /// <value>The settings for configuring the underlying HTTP transport.</value>
    public TransportOverrideOptions TransportOverride { get; set; }
}
