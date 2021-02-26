// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Routing;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Newtonsoft.Json;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public class SchemaClient : ISchemaClient, IDisposable
    {
        private HttpClient _httpClient;
        private const string Slash = "/";

        public SchemaClient()
        {
            _httpClient = new HttpClient();
        }

        public void SetUri(Uri serverUrl)
        {
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = serverUrl;
            }
        }

        public async Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken)
        {
            var currentUrl = Slash + KnownRoutes.SchemaRoot + Slash + KnownRoutes.Current;
            var response = await _httpClient.GetAsync(RelativeUrl(currentUrl), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseBodyAsString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<CurrentVersion>>(responseBodyAsString);
            }
            else
            {
                throw new SchemaManagerException(string.Format(Resources.CurrentDefaultErrorDescription, response.StatusCode));
            }
        }

        public async Task<string> GetScriptAsync(Uri scriptUri, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(scriptUri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new SchemaManagerException(string.Format(Resources.ScriptNotFound, response.StatusCode));
            }
        }

        public async Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken)
        {
            var compatibilityUrl = Slash + KnownRoutes.SchemaRoot + Slash + KnownRoutes.Compatibility;
            var response = await _httpClient.GetAsync(RelativeUrl(compatibilityUrl), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseBodyAsString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CompatibleVersion>(responseBodyAsString);
            }
            else
            {
                throw new SchemaManagerException(string.Format(Resources.CompatibilityDefaultErrorMessage, response.StatusCode));
            }
        }

        public async Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken)
        {
            var availabilityUrl = Slash + KnownRoutes.SchemaRoot + Slash + KnownRoutes.Versions;
            var response = await _httpClient.GetAsync(RelativeUrl(availabilityUrl), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseBodyAsString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<AvailableVersion>>(responseBodyAsString);
            }
            else
            {
                throw new SchemaManagerException(string.Format(Resources.AvailableVersionsDefaultErrorMessage, response.StatusCode));
            }
        }

        public async Task<string> GetDiffScriptAsync(Uri diffUri, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(diffUri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new SchemaManagerException(string.Format(Resources.ScriptNotFound, response.StatusCode));
            }
        }

        private Uri RelativeUrl(string url)
        {
            return new Uri(url, UriKind.Relative);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
