// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Exceptions;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Client.UnitTests;

public class CredentialProviderTests
{
    [Fact]
    public async Task GivenAnNonSetToken_WhenGetBearerTokenCalled_ThenBearerTokenFunctionIsCalled()
    {
        DateTime expirationTime = DateTime.UtcNow + TimeSpan.FromDays(1);

        var credentialProvider = new TestCredentialProvider(JwtTokenHelpers.GenerateToken(expirationTime));
        Assert.Null(credentialProvider.Token);
        Assert.Equal(default, credentialProvider.TokenExpiration);

        var token = await credentialProvider.GetBearerToken(cancellationToken: default);

        Assert.Equal(token, credentialProvider.Token);

        // JWT token expiration is limited to second precision
        Assert.InRange(credentialProvider.TokenExpiration, expirationTime.AddSeconds(-1), expirationTime.AddSeconds(1));
    }

    [Fact]
    public async Task InvalidOAuth2ClientCredential_RetrieveToken_ShouldThrowError()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(@"{""error"": ""This is an error!""}"),
        };

        HttpMessageHandler mockHandler = GetMockMessageHandler(
            Arg.Any<HttpRequestMessage>(),
            Arg.Any<CancellationToken>(),
            response);

        var httpClient = new HttpClient(mockHandler);

        var credentialConfiguration = new OAuth2ClientCredentialConfiguration(
                    new Uri("https://fakehost/connect/token"),
                    "invalid resource",
                    "invalid scope",
                    "invalid client id",
                    "invalid client secret");

        var credentialProvider = new OAuth2ClientCredentialProvider(GetOptionsMonitor(credentialConfiguration), httpClient);
        await Assert.ThrowsAsync<FailToRetrieveTokenException>(() => credentialProvider.GetBearerToken(cancellationToken: default));
    }

    [Fact]
    public async Task InvalidOAuth2UserPasswordCredential_RetrieveToken_ShouldThrowError()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(@"{""error"": ""This is an error!""}"),
        };

        HttpMessageHandler mockHandler = GetMockMessageHandler(
            Arg.Any<HttpRequestMessage>(),
            Arg.Any<CancellationToken>(),
            response);

        var httpClient = new HttpClient(mockHandler);

        var credentialConfiguration = new OAuth2UserPasswordCredentialConfiguration(
                    new Uri("https://fakehost/connect/token"),
                    "invalid resource",
                    "invalid scope",
                    "invalid client id",
                    "invalid client secret",
                    "invalid username",
                    "invalid password");

        var credentialProvider = new OAuth2UserPasswordCredentialProvider(GetOptionsMonitor(credentialConfiguration), httpClient);
        await Assert.ThrowsAsync<FailToRetrieveTokenException>(() => credentialProvider.GetBearerToken(cancellationToken: default));
    }

    [Fact]
    public async Task GivenANonExpiredToken_WhenGetBearerTokenCalled_ThenSameBearerTokenIsReturned()
    {
        DateTime initialExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
        var credentialProvider = new TestCredentialProvider(initialToken);

        // Returns the initialToken
        var initialResult = await credentialProvider.GetBearerToken(cancellationToken: default);
        Assert.Equal(initialToken, initialResult);

        // Update the token that would be returned if BearerTokenFunction() was called
        DateTime updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
        credentialProvider.EncodedToken = secondToken;

        // Should return the initialToken since it is not within the expiration window
        var secondResult = await credentialProvider.GetBearerToken(cancellationToken: default);

        Assert.Equal(initialResult, secondResult);
    }

    [Fact]
    public async Task GivenAnExpiringToken_WhenGetBearerTokenCalled_ThenNewBearerTokenIsReturned()
    {
        DateTime initialExpiration = DateTime.UtcNow + TimeSpan.FromMinutes(4);
        var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
        var credentialProvider = new TestCredentialProvider(initialToken);

        // Returns the initialToken
        var initialResult = await credentialProvider.GetBearerToken(cancellationToken: default);
        Assert.Equal(initialToken, initialResult);

        // Update the token that will be returned since the initial token is within the expiration window
        DateTime updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
        credentialProvider.EncodedToken = secondToken;

        // Should return the initialToken since it is not within the expiration window
        var secondResult = await credentialProvider.GetBearerToken(cancellationToken: default);

        Assert.Equal(secondToken, secondResult);
        Assert.NotEqual(initialResult, secondResult);
    }

    private static HttpMessageHandler GetMockMessageHandler(HttpRequestMessage requestMessage, CancellationToken cancellationToken, HttpResponseMessage responseMessage)
    {
        HttpMessageHandler mockHandler = Substitute.For<HttpMessageHandler>();

        typeof(HttpMessageHandler)
            .GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(mockHandler, new object[] { requestMessage, cancellationToken })
            .Returns(Task.FromResult(responseMessage));

        return mockHandler;
    }

    private static IOptionsMonitor<T> GetOptionsMonitor<T>(T configuration)
    {
        var optionsMonitor = Substitute.For<IOptionsMonitor<T>>();
        optionsMonitor.CurrentValue.Returns(configuration);
        optionsMonitor.Get(default).ReturnsForAnyArgs(configuration);
        return optionsMonitor;
    }
}
