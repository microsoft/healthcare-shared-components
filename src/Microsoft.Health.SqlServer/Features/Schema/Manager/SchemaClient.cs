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

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
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
            HttpResponseMessage response = await _httpClient
                .GetAsync(KnownRoutes.RootedCurrentUri, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
#if NET5_0_OR_GREATER
                var responseBodyAsString =
                    await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                var responseBodyAsString = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);
#endif
                return JsonConvert.DeserializeObject<List<CurrentVersion>>(responseBodyAsString);
            }

            throw new SchemaManagerException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.CurrentDefaultErrorDescription,
                response.StatusCode));
        }

        public async Task<string> GetScriptAsync(Uri scriptUri, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient
                .GetAsync(scriptUri, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
#if NET5_0_OR_GREATER
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                return await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);
#endif
            }

            throw new SchemaManagerException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.ScriptNotFound,
                response.StatusCode));
        }

        public async Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient
                .GetAsync(KnownRoutes.RootedCompatibilityUri, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
#if NET5_0_OR_GREATER
                var responseBodyAsString = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);
#else
            var responseBodyAsString = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);
#endif
                return JsonConvert.DeserializeObject<CompatibleVersion>(responseBodyAsString);
            }

            throw new SchemaManagerException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.CompatibilityDefaultErrorMessage,
                response.StatusCode));
        }

        public async Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(KnownRoutes.RootedVersionsUri, cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
#if NET5_0_OR_GREATER
                var responseBodyAsString =
                    await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                var responseBodyAsString = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);
#endif
                return JsonConvert.DeserializeObject<List<AvailableVersion>>(responseBodyAsString);
            }

            throw new SchemaManagerException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.AvailableVersionsDefaultErrorMessage,
                response.StatusCode));
        }

        public async Task<string> GetDiffScriptAsync(Uri diffScriptUri, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _httpClient
                .GetAsync(diffScriptUri, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
#if NET5_0_OR_GREATER
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                return await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);
#endif
            }

            throw new SchemaManagerException(string.Format(
                CultureInfo.InvariantCulture,
                Resources.ScriptNotFound,
                response.StatusCode));
        }
    }
}
