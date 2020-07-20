// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Client.UnitTests
{
    public class TestCredentialProvider : CredentialProvider
    {
        public TestCredentialProvider(string encodedToken = null)
        {
            EncodedToken = encodedToken;
        }

        public string EncodedToken { get; set; }

        protected override Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            return Task.FromResult(EncodedToken);
        }
    }
}