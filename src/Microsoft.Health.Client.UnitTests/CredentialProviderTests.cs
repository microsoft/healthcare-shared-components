// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
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

            var token = await credentialProvider.GetBearerTokenAsync(cancellationToken: default);

            Assert.Equal(token, credentialProvider.Token);

            // JWT token expiration is limited to second precision
            Assert.InRange(credentialProvider.TokenExpiration, expirationTime.AddSeconds(-1), expirationTime.AddSeconds(1));
        }

        [Fact]
        public async Task GivenANonExpiredToken_WhenGetBearerTokenCalled_ThenSameBearerTokenIsReturned()
        {
            var initialExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
            var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
            var credentialProvider = new TestCredentialProvider(initialToken);

            // Returns the initialToken
            var initialResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default);
            Assert.Equal(initialToken, initialResult);

            // Update the token that would be returned if BearerTokenFunction() was called
            var updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
            var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
            credentialProvider.EncodedToken = secondToken;

            // Should return the initialToken since it is not within the expiration window
            var secondResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default);

            Assert.Equal(initialResult, secondResult);
        }

        [Fact]
        public async Task GivenAnExpiringToken_WhenGetBearerTokenCalled_ThenNewBearerTokenIsReturned()
        {
            var initialExpiration = DateTime.UtcNow + TimeSpan.FromMinutes(4);
            var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
            var credentialProvider = new TestCredentialProvider(initialToken);

            // Returns the initialToken
            var initialResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default);
            Assert.Equal(initialToken, initialResult);

            // Update the token that will be returned since the initial token is within the expiration window
            var updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
            var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
            credentialProvider.EncodedToken = secondToken;

            // Should return the initialToken since it is not within the expiration window
            var secondResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default);

            Assert.Equal(secondToken, secondResult);
            Assert.NotEqual(initialResult, secondResult);
        }
    }
}