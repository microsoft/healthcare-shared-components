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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests;

[TestClass]
public class WorkloadIdentityAuthenticationProviderTests
{
    private const string DefaultAuthority = "https://login.microsoftonline.com/";
    private const string DefaultResource = "https://database.windows.net/.default";

    private readonly WorkloadIdentityCredential _credential = Substitute.For<WorkloadIdentityCredential>();

    [TestMethod]
    [DataRow("https://database.windows.net")]
    [DataRow("https://database.windows.net/.default")]
    public async Task GivenDifferentResources_WhenFetchingToken_ThenNormalizeScope(string resource)
    {
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o => _credential);

        await provider.AcquireTokenAsync(MockSqlAuthenticationParameters.Create(resource: resource));

        await _credential
            .Received(1)
            .GetTokenAsync(Arg.Is<TokenRequestContext>(c => c.Scopes.Single() == DefaultResource), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    [DataRow("https://login.microsoftonline.com/foo", "https://login.microsoftonline.com/")]
    [DataRow("https://login.microsoftonline.us/", "https://login.microsoftonline.us/")]
    public async Task GivenDifferentAuthorities_WhenFetchingToken_ThenTrimAfterFinalSlash(string authority, string expected)
    {
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o =>
        {
            Assert.AreEqual(expected, o.AuthorityHost.OriginalString);
            return _credential;
        });

        SqlAuthenticationToken actual = await provider.AcquireTokenAsync(MockSqlAuthenticationParameters.Create(authority: authority));

        Assert.AreEqual(accessToken.Token, actual.AccessToken);
    }

    [TestMethod]
    public async Task GivenNoUserId_WhenFetchingToken_ThenNoClientId()
    {
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o =>
        {
            Assert.IsNull(o.ClientId);
            return _credential;
        });

        SqlAuthenticationToken actual = await provider.AcquireTokenAsync(MockSqlAuthenticationParameters.Create(userId: null));

        Assert.AreEqual(accessToken.Token, actual.AccessToken);
        Assert.AreEqual(accessToken.ExpiresOn, actual.ExpiresOn);

        await _credential
            .Received(1)
            .GetTokenAsync(Arg.Is<TokenRequestContext>(c => c.Scopes.Single() == DefaultResource), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GivenUserId_WhenFetchingToken_ThenUseAsClientId()
    {
        string UserId = Guid.NewGuid().ToString();
        AccessToken accessToken = new(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

        _credential
            .GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(accessToken);

        WorkloadIdentityAuthenticationProvider provider = new(o =>
        {
            Assert.AreEqual(UserId, o.ClientId);
            return _credential;
        });

        SqlAuthenticationToken actual = await provider.AcquireTokenAsync(MockSqlAuthenticationParameters.Create(userId: UserId));

        Assert.AreEqual(accessToken.Token, actual.AccessToken);
        Assert.AreEqual(accessToken.ExpiresOn, actual.ExpiresOn);

        await _credential
            .Received(1)
            .GetTokenAsync(Arg.Is<TokenRequestContext>(c => c.Scopes.Single() == DefaultResource), Arg.Any<CancellationToken>());
    }

    [TestMethod]
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
                () => provider.AcquireTokenAsync(MockSqlAuthenticationParameters.Create(connectionTimeout: 1)));

        static async ValueTask<AccessToken> GetTokenAsync(CallInfo callInfo)
        {
            await Task.Delay(-1, callInfo.ArgAt<CancellationToken>(1));
            return new AccessToken();
        }
    }

    [TestMethod]
    [DataRow(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, true)]
    [DataRow(SqlAuthenticationMethod.ActiveDirectoryMSI, true)]
    [DataRow(SqlAuthenticationMethod.ActiveDirectoryDefault, false)]
    public void GivenAuthenticationMethod_WhenCheckSupport_ThenReturnTrueForManagedIdentity(SqlAuthenticationMethod authenticationMethod, bool expected)
        => Assert.AreEqual(expected, new WorkloadIdentityAuthenticationProvider().IsSupported(authenticationMethod));

    private static class MockSqlAuthenticationParameters
    {
        public static SqlAuthenticationParameters Create(
            SqlAuthenticationMethod authenticationMethod = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity,
            string resource = DefaultResource,
            string authority = DefaultAuthority,
            string userId = null,
            int connectionTimeout = -1)
        {
            return new SqlAuthenticationParameters(
                authenticationMethod,
                serverName: null,
                databaseName: null,
                resource: resource,
                authority: authority,
                userId: userId,
                password: null,
                connectionId: Guid.NewGuid(),
                connectionTimeout: connectionTimeout);
        }
    }
}
