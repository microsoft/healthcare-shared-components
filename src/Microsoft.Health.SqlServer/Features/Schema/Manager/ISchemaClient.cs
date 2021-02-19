// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public interface ISchemaClient
    {
        /// <summary>
        /// Sets Uri to HttpClient base address
        /// </summary>
        /// <param name="uri">The uri to set</param>
        void SetUri(Uri uri);

        /// <summary>
        /// Gets the current schema version information
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the complete script
        /// </summary>
        /// <param name="scriptUri">A url to fetch the script</param>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<string> GetScriptAsync(Uri scriptUri, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the diff script
        /// </summary>
        /// <param name="diffScriptUri">A url to fetch the diff script</param>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<string> GetDiffScriptAsync(Uri diffScriptUri, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the compatible min and max schema version
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Fetch the available schema versions
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of Paas-specific script
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<List<PaasSchema>> GetPaasScriptAsync(CancellationToken cancellationToken);
    }
}
