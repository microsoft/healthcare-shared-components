// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Api.Configuration;
using Microsoft.Health.Api.Features.Cors;
using Microsoft.Health.Api.Modules;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Api.UnitTests.Modules;

[TestClass]
public class CorsModuleTests
{
    private readonly CorsModule _corsModule;
    private readonly CorsConfiguration _corsConfiguration = Substitute.For<CorsConfiguration>();
    private readonly IServiceCollection _servicesCollection = Substitute.For<IServiceCollection>();

    public CorsModuleTests()
    {
        var apiConfiguration = Substitute.For<IApiConfiguration>();
        apiConfiguration.Cors.Returns(_corsConfiguration);
        _corsModule = new CorsModule(apiConfiguration);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenNoValuesSet_PolicyHasOnlyDefaults()
    {
        _corsModule.Load(_servicesCollection);

        CorsPolicy corsPolicy = _corsModule.DefaultCorsPolicy;
        Assert.IsEmpty(corsPolicy.Origins);
        Assert.IsEmpty(corsPolicy.Headers);
        Assert.IsEmpty(corsPolicy.Methods);
        Assert.IsFalse(corsPolicy.SupportsCredentials);
        Assert.IsNull(corsPolicy.PreflightMaxAge);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenAllOriginsSet_PolicyHasAllowAnyOrigin()
    {
        _corsConfiguration.Origins.Add("*");
        _corsModule.Load(_servicesCollection);

        Assert.IsTrue(_corsModule.DefaultCorsPolicy.AllowAnyOrigin);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenAllMethodsSet_PolicyHasAllowAnyMethod()
    {
        _corsConfiguration.Methods.Add("*");
        _corsModule.Load(_servicesCollection);

        Assert.IsTrue(_corsModule.DefaultCorsPolicy.AllowAnyMethod);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenAllHeadersSet_PolicyHasAllowAnyHeader()
    {
        _corsConfiguration.Headers.Add("*");
        _corsModule.Load(_servicesCollection);

        Assert.IsTrue(_corsModule.DefaultCorsPolicy.AllowAnyHeader);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenAllowCredentials_PolicyHasSupportsCredentials()
    {
        _corsConfiguration.AllowCredentials = true;
        _corsModule.Load(_servicesCollection);

        Assert.IsTrue(_corsModule.DefaultCorsPolicy.SupportsCredentials);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenMaxAgeSet_PolicyHasMaxAge()
    {
        _corsConfiguration.MaxAge = 100;
        _corsModule.Load(_servicesCollection);

        Assert.AreEqual(TimeSpan.FromSeconds(100), _corsModule.DefaultCorsPolicy.PreflightMaxAge);
    }

    [TestMethod]
    public void GivenACorsConfiguration_WhenMultipleValuesSet_PolicyHasSpecifiedValues()
    {
        _corsConfiguration.Origins.Add("https://example.com");
        _corsConfiguration.Origins.Add("https://contoso");

        _corsConfiguration.Methods.Add("PATCH");
        _corsConfiguration.Methods.Add("DELETE");

        _corsConfiguration.Headers.Add("authorization");
        _corsConfiguration.Headers.Add("content-type");

        _corsModule.Load(_servicesCollection);

        Assert.AreEqual(2, _corsModule.DefaultCorsPolicy.Origins.Count);
        Assert.AreEqual(2, _corsModule.DefaultCorsPolicy.Methods.Count);
        Assert.AreEqual(2, _corsModule.DefaultCorsPolicy.Headers.Count);

        Assert.Contains("https://example.com", _corsModule.DefaultCorsPolicy.Origins);
        Assert.Contains("https://contoso", _corsModule.DefaultCorsPolicy.Origins);

        Assert.Contains("PATCH", _corsModule.DefaultCorsPolicy.Methods);
        Assert.Contains("DELETE", _corsModule.DefaultCorsPolicy.Methods);

        Assert.Contains("authorization", _corsModule.DefaultCorsPolicy.Headers);
        Assert.Contains("content-type", _corsModule.DefaultCorsPolicy.Headers);
    }
}
