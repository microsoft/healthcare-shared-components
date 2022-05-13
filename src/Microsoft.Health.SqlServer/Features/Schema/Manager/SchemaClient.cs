// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Routing;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Newtonsoft.Json;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public class SchemaClient : ISchemaClient
{
    private readonly HttpClient _httpClient;

    public SchemaClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public void SetUri(Uri uri)
    {
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = uri;
        }
    }

    public async Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(KnownRoutes.RootedCurrentUri, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<CurrentVersion>>(responseBodyAsString);
        }
        else
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.CurrentDefaultErrorDescription, response.StatusCode));
        }
    }

    public async Task<string> GetScriptAsync(int version, CancellationToken cancellationToken)
    {
        Uri rootedScriptUri = KnownRoutes.GetRootedScriptUri(version);
        HttpResponseMessage response = await _httpClient.GetAsync(rootedScriptUri, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.ScriptNotFound, response.StatusCode));
        }
    }

    public async Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(KnownRoutes.RootedCompatibilityUri, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<CompatibleVersion>(responseBodyAsString);
        }
        else
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.CompatibilityDefaultErrorMessage, response.StatusCode));
        }
    }

    public async Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(KnownRoutes.RootedVersionsUri, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            var responseBodyAsString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<AvailableVersion>>(responseBodyAsString);
        }
        else
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.AvailableVersionsDefaultErrorMessage, response.StatusCode));
        }
    }

    public async Task<string> GetDiffScriptAsync(int version, CancellationToken cancellationToken)
    {
        Uri rootedDiffUri = KnownRoutes.GetRootedDiffUri(version);
        HttpResponseMessage response = await _httpClient.GetAsync(rootedDiffUri, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.ScriptNotFound, response.StatusCode));
        }
    }
}
