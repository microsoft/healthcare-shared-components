// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests;

public class WorkloadIdentityAuthenticationProviderTests
{
    private const string DefaultAuthority = "https://login.microsoftonline.com/";
    private const string DefaultResource = "https://database.windows.net/.default";

    private readonly WorkloadIdentityCredential _credential = Substitute.For<WorkloadIdentityCredential>();

    [Theory]
    [InlineData("https://database.windows.net")]
    [InlineData("https://database.windows.net/.default")]
    public async Task GivenDifferentResources_WhenFetchingToken_ThenNormalizeScope(string resource)
    {
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o => _credential);

        await provider
            .AcquireTokenAsync(new MockSqlAuthenticationParameters(resource: resource))
            .ConfigureAwait(false);

        await _credential
            .Received(1)
            .GetTokenAsync(Arg.Is<TokenRequestContext>(c => c.Scopes.Single() == DefaultResource), Arg.Any<CancellationToken>())
            .ConfigureAwait(false);
    }

    [Theory]
    [InlineData("https://login.microsoftonline.com/foo", "https://login.microsoftonline.com/")]
    [InlineData("https://login.microsoftonline.us/", "https://login.microsoftonline.us/")]
    public async Task GivenDifferentAuthorities_WhenFetchingToken_ThenTrimAfterFinalSlash(string authority, string expected)
    {
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o =>
        {
            Assert.Equal(expected, o.AuthorityHost.OriginalString);
            return _credential;
        });

        SqlAuthenticationToken actual = await provider
            .AcquireTokenAsync(new MockSqlAuthenticationParameters(authority: authority))
            .ConfigureAwait(false);

        Assert.Equal(accessToken.Token, actual.AccessToken);
    }

    [Fact]
    public async Task GivenNoUserId_WhenFetchingToken_ThenNoClientId()
    {
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o =>
        {
            Assert.Null(o.ClientId);
            return _credential;
        });

        SqlAuthenticationToken actual = await provider
            .AcquireTokenAsync(new MockSqlAuthenticationParameters(userId: null))
            .ConfigureAwait(false);

        Assert.Equal(accessToken.Token, actual.AccessToken);
        Assert.Equal(accessToken.ExpiresOn, actual.ExpiresOn);

        await _credential
            .Received(1)
            .GetTokenAsync(Arg.Is<TokenRequestContext>(c => c.Scopes.Single() == DefaultResource), Arg.Any<CancellationToken>())
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task GivenUserId_WhenFetchingToken_ThenUseAsClientId()
    {
        string UserId = Guid.NewGuid().ToString();
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o =>
        {
            Assert.Equal(UserId, o.ClientId);
            return _credential;
        });

        SqlAuthenticationToken actual = await provider
            .AcquireTokenAsync(new MockSqlAuthenticationParameters(userId: UserId))
            .ConfigureAwait(false);

        Assert.Equal(accessToken.Token, actual.AccessToken);
        Assert.Equal(accessToken.ExpiresOn, actual.ExpiresOn);

        await _credential
            .Received(1)
            .GetTokenAsync(Arg.Is<TokenRequestContext>(c => c.Scopes.Single() == DefaultResource), Arg.Any<CancellationToken>())
            .ConfigureAwait(false);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "ValueTask only used once.")]
    public async Task GivenTimeout_WhenFetchingToken_ThenThrowException()
    {
        string UserId = Guid.NewGuid().ToString();
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(GetTokenAsync);

        WorkloadIdentityAuthenticationProvider provider = new(o => _credential);

        await Assert
            .ThrowsAsync<TaskCanceledException>(
                () => provider.AcquireTokenAsync(new MockSqlAuthenticationParameters(connectionTimeout: 1)))
            .ConfigureAwait(false);

        static async ValueTask<AccessToken> GetTokenAsync(CallInfo callInfo)
        {
            await Task.Delay(-1, callInfo.ArgAt<CancellationToken>(1)).ConfigureAwait(false);
            return new AccessToken();
        }
    }

    [Theory]
    [InlineData(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, true)]
    [InlineData(SqlAuthenticationMethod.ActiveDirectoryMSI, true)]
    [InlineData(SqlAuthenticationMethod.ActiveDirectoryDefault, false)]
    public void GivenAuthenticationMethod_WhenCheckSupport_ThenReturnTrueForManagedIdentity(SqlAuthenticationMethod authenticationMethod, bool expected)
        => Assert.Equal(expected, new WorkloadIdentityAuthenticationProvider().IsSupported(authenticationMethod));

    private sealed class MockSqlAuthenticationParameters : SqlAuthenticationParameters
    {
        public MockSqlAuthenticationParameters(
            SqlAuthenticationMethod authenticationMethod = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity,
            string resource = DefaultResource,
            string authority = DefaultAuthority,
            string userId = null,
            int connectionTimeout = -1)
            : base(
                  authenticationMethod,
                  serverName: null,
                  databaseName: null,
                  resource: resource,
                  authority: authority,
                  userId: userId,
                  password: null,
                  connectionId: Guid.NewGuid(),
                  connectionTimeout: connectionTimeout)
        { }
    }
}
