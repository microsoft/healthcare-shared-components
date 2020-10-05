// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;

namespace Microsoft.Health.SqlServer
{
    public class ManagedIdentityAccessTokenHandler : IAccessTokenHandler
    {
        private readonly AzureServiceTokenProvider _azureServiceTokenProvider;

        public ManagedIdentityAccessTokenHandler()
        {
            _azureServiceTokenProvider = new AzureServiceTokenProvider();
        }

        public Task<string> GetAccessTokenAsync()
        {
            return _azureServiceTokenProvider.GetAccessTokenAsync("https://database.windows.net/");
        }
    }
}
