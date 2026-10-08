// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Exceptions;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Api.UnitTests.Features.Audit;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Values do not need to be cryptographically secure.")]
[TestClass]
public class AuditHeaderReaderTests
{
    private readonly DefaultHttpContext _httpContext;
    private readonly IOptions<AuditConfiguration> _optionsAuditConfiguration;
    private const string CustomAuditHeaderPrefix = "X-MS-AZUREHEALTH-AUDIT-";

    public AuditHeaderReaderTests()
    {
        _httpContext = new DefaultHttpContext();

        var auditConfiguration = new AuditConfiguration()
        {
            CustomAuditHeaderPrefix = CustomAuditHeaderPrefix,
        };

        _optionsAuditConfiguration = Substitute.For<IOptions<AuditConfiguration>>();
        _optionsAuditConfiguration.Value.Returns(auditConfiguration);
    }

    [TestMethod]
    public void GivenNoCustomHeaders_WhenHeadersRead_ThenEmptyDictionaryReturned()
    {
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);

        // No headers at all
        var result = headerReader.Read(_httpContext);
        Assert.IsEmpty(result);

        // Some non-audit headers
        var headers = GenerateRandomHeaders(1, 0).ToList()[0][0] as Dictionary<string, string>;
        foreach (var header in headers)
        {
            _httpContext.Request.Headers.Append(header.Key, new StringValues(header.Value));
        }

        result = headerReader.Read(_httpContext);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    [DynamicData(nameof(GenerateRandomHeaders), 10, -1)]
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Headers are not null.")]
    public void GivenMixedHeaders_WhenHeadersRead_ThenOnlyCorrectCustomHeadersReturn(IReadOnlyDictionary<string, string> headers, int expectedCustomHeaderCount)
    {
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);
        _httpContext.Request.Headers.Clear();

        foreach (var header in headers)
        {
            _httpContext.Request.Headers.Append(header.Key, new StringValues(header.Value));
        }

        var result = headerReader.Read(_httpContext);
        Assert.AreEqual(expectedCustomHeaderCount, result.Count);
    }

    [TestMethod]
    [SuppressMessage("Usage", "ASP0019:Suggest using IHeaderDictionary.Append or the indexer", Justification = "Desired behavior adds empty headers.")]
    public void GivenHeaderWithNoValue_WhenHeadersRead_ThenHeaderNameWithEmptyValueIsReturned()
    {
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);
        var headers = GenerateRandomHeaders(1, 5).ToList()[0][0] as Dictionary<string, string>;
        foreach (var header in headers)
        {
            _httpContext.Request.Headers.Add(header.Key, default);
        }

        var result = headerReader.Read(_httpContext);

        Assert.AreEqual(5, result.Count);

        foreach (var customHeader in result)
        {
            Assert.IsTrue(string.IsNullOrEmpty(customHeader.Value));
        }
    }

    [TestMethod]
    public void GivenMultipleValuesOfSameHeader_WhenHeadersRead_ThenConcatenatedStringValueReturend()
    {
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);
        _httpContext.Request.Headers.Append(_optionsAuditConfiguration.Value.CustomAuditHeaderPrefix + "repeated", new StringValues(["item1", "item2"]));

        var result = headerReader.Read(_httpContext);
        Assert.AreEqual("item1,item2", result[_optionsAuditConfiguration.Value.CustomAuditHeaderPrefix + "repeated"]);
    }

    [TestMethod]
    public void GivenTooManyCustomHeaders_WhenHeadersRead_ThenAuditHeaderCountExceededExceptionIsThrown()
    {
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);
        _httpContext.Request.Headers.Clear();
        var headers = GenerateRandomHeaders(1, 11).ToList()[0][0] as Dictionary<string, string>;
        foreach (var header in headers)
        {
            _httpContext.Request.Headers.Append(header.Key, new StringValues(header.Value));
        }

        Assert.Throws<AuditHeaderCountExceededException>(() => headerReader.Read(_httpContext));
    }

    [TestMethod]
    public void GivenAHeaderWithTooLargeValue_WhenHeadersRead_ThenAuditHeaderTooLargeExceptionIsThrown()
    {
        var d = new Dictionary<string, string>() { ["a"] = "b" };
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);
        _httpContext.Request.Headers.Clear();
        var headers = GenerateRandomHeaders(1, 9).ToList()[0][0] as Dictionary<string, string>;
        foreach (var header in headers)
        {
            _httpContext.Request.Headers.Append(header.Key, new StringValues(header.Value));
        }

        _httpContext.Request.Headers.Append(_optionsAuditConfiguration.Value.CustomAuditHeaderPrefix + "big", GenerateRandomString(2049));

        Assert.Throws<AuditHeaderTooLargeException>(() => headerReader.Read(_httpContext));
    }

    [TestMethod]
    public void GivenCustomHeaders_WhenMultipleCallsUsingTheSameHttpContext_ThenHttpContextItemsIsUsed()
    {
        var headerReader = new AuditHeaderReader(_optionsAuditConfiguration);

        var headers = GenerateRandomHeaders(1, 5).ToList()[0][0] as Dictionary<string, string>;
        foreach (var header in headers)
        {
            _httpContext.Request.Headers.Append(header.Key, new StringValues(header.Value));
        }

        var result = headerReader.Read(_httpContext);
        Assert.HasCount(5, result);

        var changedHeaders = new Dictionary<string, string>() { ["changed"] = "changed" };

        _httpContext.Items[AuditConstants.CustomAuditHeaderKeyValue] = changedHeaders;

        result = headerReader.Read(_httpContext);
        Assert.AreEquivalent(changedHeaders, result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void GivenEmptyPrefixValue_WhenConfigurationValueSet_ThenInvalidDefinitionExceptionIsThrown(string prefix)
    {
        var auditConfiguration = new AuditConfiguration();
        Assert.Throws<InvalidDefinitionException>(() => auditConfiguration.CustomAuditHeaderPrefix = prefix);
    }

    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Public member used for test data.")]
    public static List<object[]> GenerateRandomHeaders(int testCount, int numberOfAuditHeaders)
    {
        var tests = new List<object[]>();
        var random = new Random();
        for (var testIndex = 0; testIndex < testCount; testIndex++)
        {
            if (numberOfAuditHeaders == -1)
            {
                numberOfAuditHeaders = random.Next(1, 10);
            }

            var headers = new Dictionary<string, string>();
            for (var headerIndex = 0; headerIndex < numberOfAuditHeaders; headerIndex++)
            {
                var headerName = new StringBuilder(CustomAuditHeaderPrefix);
                headerName.Append(GenerateRandomString(20));
                headers[headerName.ToString()] = GenerateRandomString(random.Next(1, 2048));
            }

            var numberOfOtherHeaders = random.Next(1, 10);
            for (var headerIndex = 0; headerIndex < numberOfOtherHeaders; headerIndex++)
            {
                headers[GenerateRandomString(10)] = GenerateRandomString(random.Next(1, 2048));
            }

            tests.Add([headers, numberOfAuditHeaders]);
        }

        return tests;
    }

    private static string GenerateRandomString(int length)
    {
        var random = new Random();
        var randomChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            randomChars[i] = (char)random.Next(65, 126);
        }

        return new string(randomChars);
    }
}
