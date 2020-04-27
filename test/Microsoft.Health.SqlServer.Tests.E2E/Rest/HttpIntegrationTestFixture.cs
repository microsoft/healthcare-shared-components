﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.SqlServer.Tests.E2E.Rest
{
    public class HttpIntegrationTestFixture<TStartup> : IDisposable
    {
        private readonly string _environmentUrl;
        private readonly HttpMessageHandler _messageHandler;

        public HttpIntegrationTestFixture()
            : this(Path.Combine("test"))
        {
        }

        protected HttpIntegrationTestFixture(string targetProjectParentDirectory)
        {
            string environmentUrl = Environment.GetEnvironmentVariable("TestEnvironmentUrl");

            if (string.IsNullOrWhiteSpace(environmentUrl))
            {
                environmentUrl = "http://localhost/";

                StartInMemoryServer(targetProjectParentDirectory);

                _messageHandler = Server.CreateHandler();
                IsUsingInProcTestServer = true;
            }
            else
            {
                if (environmentUrl.Last() != '/')
                {
                    environmentUrl = $"{environmentUrl}/";
                }

                _messageHandler = new HttpClientHandler();
            }

            _environmentUrl = environmentUrl;

            HttpClient = CreateHttpClient();

            RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

            Client = HttpClient;
        }

        public bool IsUsingInProcTestServer { get; }

        public HttpClient HttpClient { get; }

        protected TestServer Server { get; private set; }

        public RecyclableMemoryStreamManager RecyclableMemoryStreamManager { get; }

        public HttpClient Client { get; }

        public HttpClient CreateHttpClient()
            => new HttpClient(new SessionMessageHandler(_messageHandler)) { BaseAddress = new Uri(_environmentUrl) };

        public void Dispose()
        {
            HttpClient.Dispose();
            Server?.Dispose();
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

            throw new Exception($"Project root could not be located for startup type {startupType.FullName}");
        }

        private void StartInMemoryServer(string targetProjectParentDirectory)
        {
            var contentRoot = GetProjectPath(targetProjectParentDirectory, typeof(TStartup));
            var projectDir = GetProjectPath("test", typeof(TStartup));

            var launchSettings = JObject.Parse(File.ReadAllText(Path.Combine(projectDir, "Properties", "launchSettings.json")));

            var configuration = launchSettings["profiles"]["SqlServer"]["environmentVariables"].Cast<JProperty>().ToDictionary(p => p.Name, p => p.Value.ToString());

            var builder = WebHost.CreateDefaultBuilder()
                .UseContentRoot(contentRoot)
                .ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddInMemoryCollection(configuration);
                })
                .UseStartup(typeof(TStartup));

            Server = new TestServer(builder);
        }

        /// <summary>
        /// An <see cref="HttpMessageHandler"/> that maintains session consistency between requests.
        /// </summary>
        private class SessionMessageHandler : DelegatingHandler
        {
            public SessionMessageHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }
        }
    }
}
