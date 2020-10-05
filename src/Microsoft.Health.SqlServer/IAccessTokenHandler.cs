// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer
{
    public interface IAccessTokenHandler
    {
        /// <summary>
        /// Get access token for the resource.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <returns>Task returning token.</returns>
        Task<string> GetAccessTokenAsync(string resource);
    }
}
