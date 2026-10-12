// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Exceptions;
using Microsoft.Health.Core.Features.Security;
using Microsoft.Health.Core.UnitTests.Features.Security.Samples;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Core.UnitTests.Features.Security;

[TestClass]
public class RoleLoaderTests
{
    private static readonly string[] AllDataActions = ["*"];
    private static readonly string[] DefaultScopes = ["/"];

    public static IEnumerable<(string, object)> GetInvalidRoles()
    {
        yield return
        (
            "empty name",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = string.Empty,
                        dataActions = AllDataActions,
                        notDataActions = Array.Empty<string>(),
                        scopes = DefaultScopes,
                    },
                },
            }
        );

        yield return
        (
            "actions missing",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = "abc",
                        notDataActions = Array.Empty<string>(),
                        scopes = DefaultScopes,
                    },
                },
            }
        );

        yield return
        (
            "invalid notAction",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = "abc",
                        dataActions = AllDataActions,
                        notDataActions = new[] { "abc" },
                        scopes = DefaultScopes,
                    },
                },
            }
        );

        yield return
        (
            "missing scopes",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = "abc",
                        dataActions = AllDataActions,
                        notDataActions = Array.Empty<string>(),
                    },
                },
            }
        );

        yield return
        (
            "scope not /",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = "abc",
                        dataActions = AllDataActions,
                        notDataActions = Array.Empty<string>(),
                        scopes = new[] { "/a" },
                    },
                },
            }
        );

        yield return
        (
            "scope not single /",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = "abc",
                        dataActions = AllDataActions,
                        notDataActions = Array.Empty<string>(),
                        scopes = new[] { "/", "/" },
                    },
                },
            }
        );

        yield return
        (
            "role name duplicated",
            new
            {
                roles = new[]
                {
                    new
                    {
                        name = "abc",
                        dataActions = AllDataActions,
                        notDataActions = Array.Empty<string>(),
                        scopes = DefaultScopes,
                    },
                    new
                    {
                        name = "abc",
                        dataActions = AllDataActions,
                        notDataActions = Array.Empty<string>(),
                        scopes = DefaultScopes,
                    },
                },
            }
        );
    }

    [TestMethod]
    public async Task GivenValidRoles_WhenLoaded_AreProperlyTransformed()
    {
        var roles = new
        {
            roles = new[]
            {
                new
                {
                    name = "x",
                    dataActions = AllDataActions,
                    notDataActions = Array.Empty<string>(),
                    scopes = DefaultScopes,
                },
            },
        };

        AuthorizationConfiguration<DataActions> authConfig = await LoadAsync(roles);

        Role<DataActions> actualRole = Assert.ContainsSingle(authConfig.Roles);
        Assert.AreEqual(roles.roles.First().name, actualRole.Name);
        Assert.AreEqual(DataActions.All, actualRole.AllowedDataActions);
    }

    [TestMethod]
    public async Task GivenValidDataActions_WhenSpecifiedAsRoleActions_AreRecognized()
    {
        IEnumerable<DataActions> actionNames = Enum.GetValues<DataActions>()
            .Cast<DataActions>()
            .Where(a => a != DataActions.All && a != DataActions.None);

        var roles = new
        {
            roles = actionNames.Select(a =>
                new
                {
                    name = $"role{a}",
                    dataActions = new[] { char.ToLowerInvariant(a.ToString()[0]) + a.ToString()[1..] },
                    notDataActions = Array.Empty<string>(),
                    scopes = DefaultScopes,
                }).ToArray(),
        };

        AuthorizationConfiguration<DataActions> authConfig = await LoadAsync(roles);

        foreach ((DataActions first, DataActions second) in actionNames.Zip(authConfig.Roles.Select(r => r.AllowedDataActions)))
            Assert.AreEqual(first, second);
    }

    [TestMethod]
    [DynamicData(nameof(GetInvalidRoles))]
    public async Task GivenInvalidRoles_WhenLoaded_RaiseValidationErrors(string description, object roles)
    {
        Assert.IsNotEmpty(description);
        await Assert.ThrowsAsync<InvalidDefinitionException>(() => LoadAsync(roles));
    }

    private static async Task<AuthorizationConfiguration<DataActions>> LoadAsync(object roles)
    {
        using var result = new MemoryStream(Encoding.UTF8.GetBytes(JObject.FromObject(roles).ToString()));

        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootFileProvider
            .GetFileInfo("roles.json")
            .CreateReadStream()
            .Returns(result);

        var authConfig = new AuthorizationConfiguration<DataActions>();
        var roleLoader = new SamplesRoleLoader(authConfig, hostEnvironment);
        await roleLoader.StartAsync(CancellationToken.None);
        return authConfig;
    }
}
