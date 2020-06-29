// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Client.UnitTests
{
    public class AuthenticationHttpMessageHandlerTests
    {
        private readonly AuthenticationHttpMessageHandler _authenticationHttpMessageHandler;

        public AuthenticationHttpMessageHandlerTests()
        {
            var credentialProvider = Substitute.For<ICredentialProvider>();
            credentialProvider.GetBearerToken(Arg.Any<CancellationToken>()).Returns("token");
            _authenticationHttpMessageHandler = new AuthenticationHttpMessageHandler(credentialProvider)
            {
                InnerHandler = new TestInnerHandler(),
            };
        }

        [Fact]
        public async Task GivenARequest_WhenSendAsyncCalled_AuthorizationHeaderIsSet()
        {
            var invoker = new HttpMessageInvoker(_authenticationHttpMessageHandler);
            var result = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            Assert.Equal("Bearer", result.RequestMessage.Headers.Authorization.Scheme);
            Assert.Equal("token", result.RequestMessage.Headers.Authorization.Parameter);
        }
    }
}