// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Client.Authentication;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Client.UnitTests;

public sealed class AuthenticationHttpMessageHandlerTests : IDisposable
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
        using var invoker = new HttpMessageInvoker(_authenticationHttpMessageHandler);
        using var message = new HttpRequestMessage();

        var result = await invoker.SendAsync(message, CancellationToken.None);

        Assert.Equal("Bearer", result.RequestMessage.Headers.Authorization.Scheme);
        Assert.Equal("token", result.RequestMessage.Headers.Authorization.Parameter);
    }

    public void Dispose()
    {
        _authenticationHttpMessageHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
