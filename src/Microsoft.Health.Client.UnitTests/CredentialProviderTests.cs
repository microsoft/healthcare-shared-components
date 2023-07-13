// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Authentication;
using Microsoft.Health.Client.Authentication.Exceptions;
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

        var token = await credentialProvider.GetBearerTokenAsync(cancellationToken: default).ConfigureAwait(false);

        Assert.Equal(token, credentialProvider.Token);

        // JWT token expiration is limited to second precision
        Assert.InRange(credentialProvider.TokenExpiration, expirationTime.AddSeconds(-1), expirationTime.AddSeconds(1));
    }

    [Fact]
    public async Task InvalidOAuth2ClientCredential_RetrieveToken_ShouldThrowError()
    {
        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(@"{""error"": ""This is an error!""}"),
        };

        HttpMessageHandler mockHandler = GetMockMessageHandler(
            Arg.Any<HttpRequestMessage>(),
            response,
            Arg.Any<CancellationToken>());

        using var httpClient = new HttpClient(mockHandler);

        var credentialConfiguration = new OAuth2ClientCredentialOptions(
                    new Uri("https://fakehost/connect/token"),
                    "invalid resource",
                    "invalid scope",
                    "invalid client id",
                    "invalid client secret");

        var credentialProvider = new OAuth2ClientCredentialProvider(GetOptionsMonitor(credentialConfiguration), httpClient);
        await Assert.ThrowsAsync<FailToRetrieveTokenException>(() => credentialProvider.GetBearerTokenAsync(cancellationToken: default)).ConfigureAwait(false);
    }

    [Fact]
    public async Task InvalidOAuth2UserPasswordCredential_RetrieveToken_ShouldThrowError()
    {
        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent(@"{""error"": ""This is an error!""}"),
        };

        HttpMessageHandler mockHandler = GetMockMessageHandler(
            Arg.Any<HttpRequestMessage>(),
            response,
            Arg.Any<CancellationToken>());

        using var httpClient = new HttpClient(mockHandler);

        var credentialConfiguration = new OAuth2UserPasswordCredentialOptions(
                    new Uri("https://fakehost/connect/token"),
                    "invalid resource",
                    "invalid scope",
                    "invalid client id",
                    "invalid client secret",
                    "invalid username",
                    "invalid password");

        var credentialProvider = new OAuth2UserPasswordCredentialProvider(GetOptionsMonitor(credentialConfiguration), httpClient);
        await Assert.ThrowsAsync<FailToRetrieveTokenException>(() => credentialProvider.GetBearerTokenAsync(cancellationToken: default)).ConfigureAwait(false);
    }

    [Fact]
    public async Task GivenANonExpiredToken_WhenGetBearerTokenCalled_ThenSameBearerTokenIsReturned()
    {
        DateTime initialExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
        var credentialProvider = new TestCredentialProvider(initialToken);

        // Returns the initialToken
        var initialResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default).ConfigureAwait(false);
        Assert.Equal(initialToken, initialResult);

        // Update the token that would be returned if BearerTokenFunction() was called
        DateTime updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
        credentialProvider.EncodedToken = secondToken;

        // Should return the initialToken since it is not within the expiration window
        var secondResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default).ConfigureAwait(false);

        Assert.Equal(initialResult, secondResult);
    }

    [Fact]
    public async Task GivenAnExpiringToken_WhenGetBearerTokenCalled_ThenNewBearerTokenIsReturned()
    {
        DateTime initialExpiration = DateTime.UtcNow + TimeSpan.FromMinutes(4);
        var initialToken = JwtTokenHelpers.GenerateToken(initialExpiration);
        var credentialProvider = new TestCredentialProvider(initialToken);

        // Returns the initialToken
        var initialResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default).ConfigureAwait(false);
        Assert.Equal(initialToken, initialResult);

        // Update the token that will be returned since the initial token is within the expiration window
        DateTime updatedExpiration = DateTime.UtcNow + TimeSpan.FromDays(1);
        var secondToken = JwtTokenHelpers.GenerateToken(updatedExpiration);
        credentialProvider.EncodedToken = secondToken;

        // Should return the initialToken since it is not within the expiration window
        var secondResult = await credentialProvider.GetBearerTokenAsync(cancellationToken: default).ConfigureAwait(false);

        Assert.Equal(secondToken, secondResult);
        Assert.NotEqual(initialResult, secondResult);
    }

    [Fact]
    public void GivenACertificateWithAPrivateKey_WhenGeneratingClientAssertion_ThenPrivateKeyNotIncludedInX5c()
    {
        string clientId = Guid.NewGuid().ToString();
        using var certificate = BuildSelfSignedServerCertificate(clientId);

        Assert.True(certificate.HasPrivateKey);

        var assertion = OAuth2ClientCertificateCredentialProvider.GenerateClientAssertion(clientId, certificate, new Uri("https://example.com/token"));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadToken(assertion) as JwtSecurityToken;

        Assert.NotNull(token?.Header.X5c);

        byte[] x5CBytes = Convert.FromBase64String(token.Header.X5c);
        using var x5CCertificate = new X509Certificate2(x5CBytes);

        Assert.Equal($"CN={clientId}", x5CCertificate.SubjectName.Name);
        Assert.False(x5CCertificate.HasPrivateKey);

        static X509Certificate2 BuildSelfSignedServerCertificate(string certificateName)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("example.com");
            sanBuilder.AddDnsName(Environment.MachineName);

            var distinguishedName = new X500DistinguishedName($"CN={certificateName}");

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            request.CertificateExtensions.Add(sanBuilder.Build());

            X509Certificate2 certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(1)));

            return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "exampleString"), "exampleString", X509KeyStorageFlags.MachineKeySet);
        }
    }

    private static HttpMessageHandler GetMockMessageHandler(HttpRequestMessage requestMessage, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
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
