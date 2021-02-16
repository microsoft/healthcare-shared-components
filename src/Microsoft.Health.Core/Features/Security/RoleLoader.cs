// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Core.Configs;
using Microsoft.Health.Core.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Health.Core.Features.Security
{
    /// <summary>
    /// Reads in roles from roles.json and validates against roles.json.schema and then
    /// sets <see cref="AuthorizationConfiguration.Roles"/>.
    /// We do not use asp.net configuration for reading in these settings
    /// because the binder provides no error handling (and its merging
    /// behavior when multiple config providers set array elements can
    /// lead to unexpected results)
    /// </summary>
    /// <typeparam name="TEnum">Type representing the dataActions for the service</typeparam>
    public abstract class RoleLoader<TEnum> : IHostedService
        where TEnum : Enum
    {
        private readonly AuthorizationConfiguration<TEnum> _authorizationConfiguration;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly Microsoft.Extensions.FileProviders.IFileProvider _fileProvider;

        public RoleLoader(AuthorizationConfiguration<TEnum> authorizationConfiguration, IHostEnvironment hostEnvironment)
        {
            EnsureArg.IsNotNull(authorizationConfiguration, nameof(authorizationConfiguration));
            EnsureArg.IsNotNull(hostEnvironment, nameof(hostEnvironment));
            EnsureArg.IsNotNull(hostEnvironment.ContentRootFileProvider, nameof(hostEnvironment.ContentRootFileProvider));

            _authorizationConfiguration = authorizationConfiguration;
            _hostEnvironment = hostEnvironment;
            _fileProvider = hostEnvironment.ContentRootFileProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using Stream schemaContents = GetType().Assembly.GetManifestResourceStream(GetType(), "roles.schema.json");

            using Stream rolesContents = _fileProvider.GetFileInfo("roles.json").CreateReadStream();

            var jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings { Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) } });

            using var schemaReader = new JsonTextReader(new StreamReader(schemaContents));
            using var validatingReader = new JSchemaValidatingReader(new JsonTextReader(new StreamReader(rolesContents)))
            {
                Schema = JSchema.Load(schemaReader),
            };

            validatingReader.ValidationEventHandler += (sender, args) =>
                throw new InvalidDefinitionException(string.Format(Resources.ErrorValidatingRoles, args.Message));

            RolesContract rolesContract = jsonSerializer.Deserialize<RolesContract>(validatingReader);

            _authorizationConfiguration.Roles = rolesContract.Roles.Select(RoleContractToRole).ToArray();

            // validate that names are all unique
            foreach (IGrouping<string, Role<TEnum>> grouping in _authorizationConfiguration.Roles.GroupBy(r => r.Name))
            {
                if (grouping.Count() > 1)
                {
                    throw new InvalidDefinitionException(
                        string.Format(CultureInfo.CurrentCulture, Resources.DuplicateRoleNames, grouping.Count(), grouping.Key));
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected abstract Role<TEnum> RoleContractToRole(RoleContract roleContract);

        private class RolesContract
        {
            public RoleContract[] Roles { get; set; }
        }
    }
}