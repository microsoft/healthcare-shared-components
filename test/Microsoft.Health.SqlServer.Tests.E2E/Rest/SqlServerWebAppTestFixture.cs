// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.SqlServer.Web.Hosting;
using Microsoft.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.SqlServer.Tests.E2E.Rest;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Used by test framework.")]
public class SqlServerWebAppTestFixture : IAsyncDisposable
{
    private bool _isDisposed;

    public SqlServerWebAppTestFixture()
        : this(Path.Combine("test"))
    {
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "DelegatingHandler disposes inner handler.")]
    protected SqlServerWebAppTestFixture(string targetProjectParentDirectory)
    {
        string environmentUrl = Environment.GetEnvironmentVariable("TestEnvironmentUrl");

        if (string.IsNullOrWhiteSpace(environmentUrl))
        {
            environmentUrl = "http://localhost/";

            WebApplication = CreateLocalWebApp(targetProjectParentDirectory);
            WebApplication.Start();

            HttpClient = WebApplication.GetTestClient();
            IsUsingInProcTestServer = true;
        }
        else
        {
            if (environmentUrl.Last() != '/')
            {
                environmentUrl = $"{environmentUrl}/";
            }

            HttpClient = new HttpClient() { BaseAddress = new Uri(environmentUrl) };
        }

        RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        Client = HttpClient;
    }

    public bool IsUsingInProcTestServer { get; }

    public HttpClient HttpClient { get; }

    protected WebApplication WebApplication { get; private set; }

    public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

    public HttpClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                HttpClient.Dispose();

                if (WebApplication is not null)
                {
                    await WebApplication.StopAsync();
                    await WebApplication.DisposeAsync();
                }
            }

            _isDisposed = true;
        }
    }

    /// <summary>
    /// Gets the full path to the target project that we wish to test
    /// </summary>
    /// <param name="projectRelativePath">
    /// The parent directory of the target project.
    /// e.g. src, samples, test, or test/Websites
    /// </param>
    /// <param name="startupType">The startup type</param>
    /// <returns>The full path to the target project.</returns>
    private static string GetProjectPath(string projectRelativePath, Type startupType)
    {
        for (Type type = startupType; type != null; type = type.BaseType)
        {
            // Get name of the target project which we want to test
            var projectName = type.GetTypeInfo().Assembly.GetName().Name;

            // Get currently executing test project path
            var applicationBasePath = AppContext.BaseDirectory;

            // Find the path to the target project
            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                directoryInfo = directoryInfo.Parent;

                var projectDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, projectRelativePath));
                if (projectDirectoryInfo.Exists)
                {
                    var projectFileInfo = new FileInfo(Path.Combine(projectDirectoryInfo.FullName, projectName, $"{projectName}.csproj"));
                    if (projectFileInfo.Exists)
                    {
                        return Path.Combine(projectDirectoryInfo.FullName, projectName);
                    }
                }
            }
            while (directoryInfo.Parent != null);
        }

        throw new ArgumentException($"Project root could not be located for startup type {startupType.FullName}", nameof(startupType));
    }

    private static WebApplication CreateLocalWebApp(string targetProjectParentDirectory)
    {
        var contentRoot = GetProjectPath(targetProjectParentDirectory, typeof(SqlServerApplicationHostingExtensions));
        var projectDir = GetProjectPath("test", typeof(SqlServerApplicationHostingExtensions));

        var launchSettings = JObject.Parse(File.ReadAllText(Path.Combine(projectDir, "Properties", "launchSettings.json")));

        var configuration = launchSettings["profiles"]["Microsoft.Health.SqlServer.Web"]["environmentVariables"].Cast<JProperty>().ToDictionary(p => p.Name, p => p.Value.ToString());

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRoot,
        });

        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.ConfigureSqlServerWebServices();
        builder.WebHost.UseTestServer();

        WebApplication webApp = builder.Build();
        webApp.ConfigureSqlServerWebApp();

        return webApp;
    }
}
