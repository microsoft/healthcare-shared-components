// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Exceptions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Health.Client.UnitTests
{
    public class CredentialProviderTests
    {
        [Fact]
        public async Task GivenAnNonSetToken_WhenGetBearerTokenCalled_ThenBearerTokenFunctionIsCalled()
        {
            var expirationTime = DateTime.UtcNow + TimeSpan.FromDays(1);

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
            var mockHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(@"{""error"": ""This is an error!""}"),
            };

            mockHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);

            var httpClient = new HttpClient(mockHandler.Object);

            var credentialConfiguration = new OAuth2ClientCredentialConfiguration(
                        new Uri("https://fakehost/connect/token"),
                        "invalid resource",
                        "invalid scope",
                        "invalid client id",
                        "invalid client secret");
            var credentialProvider = new OAuth2ClientCredentialProvider(Options.Create(credentialConfiguration), httpClient);
            await Assert.ThrowsAsync<FailToRetrieveTokenException>(() => credentialProvider.GetBearerToken(cancellationToken: default));
        }

        [Fact]
        public async Task InvalidOAuth2UserPasswordCredential_RetrieveToken_ShouldThrowError()
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(@"{""error"": ""This is an error!""}"),
            };

            mockHandler
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);

            var httpClient = new HttpClient(mockHandler.Object);

            var credentialConfiguration = new OAuth2UserPasswordCredentialConfiguration(
                        new Uri("https://fakehost/connect/token"),
                        "invalid resource",
                        "invalid scope",
                        "invalid client id",
                        "invalid client secret",
                        "invalid username",
                        "invaid password");
            var credentialProvider = new OAuth2UserPasswordCredentialProvider(Options.Create(credentialConfiguration), httpClient);
            await Assert.ThrowsAsync<FailToRetrieveTokenException>(() => credentialProvider.GetBearerToken(cancellationToken: default));
        }

        [Fact]
        public async Task GivenANonExpiredToken_WhenGetBearerTokenCalled_ThenSameBearerTokenIsReturned()
        {
            var initialExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
            var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
            var credentialProvider = new TestCredentialProvider(initialToken);

            // Returns the initialToken
            var initialResult = await credentialProvider.GetBearerToken(cancellationToken: default);
            Assert.Equal(initialToken, initialResult);

            // Update the token that would be returned if BearerTokenFunction() was called
            var updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
            var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
            credentialProvider.EncodedToken = secondToken;

            // Should return the initialToken since it is not within the expiration window
            var secondResult = await credentialProvider.GetBearerToken(cancellationToken: default);

            Assert.Equal(initialResult, secondResult);
        }

        [Fact]
        public async Task GivenAnExpiringToken_WhenGetBearerTokenCalled_ThenNewBearerTokenIsReturned()
        {
            var initialExpiration = DateTime.UtcNow + TimeSpan.FromMinutes(4);
            var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
            var credentialProvider = new TestCredentialProvider(initialToken);

            // Returns the initialToken
            var initialResult = await credentialProvider.GetBearerToken(cancellationToken: default);
            Assert.Equal(initialToken, initialResult);

            // Update the token that will be returned since the initial token is within the expiration window
            var updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
            var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
            credentialProvider.EncodedToken = secondToken;

            // Should return the initialToken since it is not within the expiration window
            var secondResult = await credentialProvider.GetBearerToken(cancellationToken: default);

            Assert.Equal(secondToken, secondResult);
            Assert.NotEqual(initialResult, secondResult);
        }
    }
}