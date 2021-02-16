// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public class CurrentCommand : Command
    {
        private readonly BaseSchemaRunner _baseSchemaRunner;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStore;
        private readonly ISchemaClient _schemaClient;

        public CurrentCommand(
            BaseSchemaRunner baseSchemaRunner,
            SqlServerDataStoreConfiguration sqlServerDataStore,
            ISchemaClient schemaClient)
            : base(CommandNames.Current, Resources.CurrentCommandDescription)
        {
            AddOption(CommandOptions.ServerOption());
            AddOption(CommandOptions.ConnectionStringOption());

            Handler = CommandHandler.Create(
                (InvocationContext context, Uri server, string connectionString, CancellationToken token)
                => HandlerAsync(context, server, connectionString, token));

            Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, CommandOptions.ConnectionStringOption(), Resources.ConnectionStringRequiredValidation));
            Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));

            EnsureArg.IsNotNull(baseSchemaRunner);
            EnsureArg.IsNotNull(sqlServerDataStore);
            EnsureArg.IsNotNull(schemaClient);

            _baseSchemaRunner = baseSchemaRunner;
            _sqlServerDataStore = sqlServerDataStore;
            _schemaClient = schemaClient;
        }

        private async Task HandlerAsync(InvocationContext invocationContext, Uri server, string connectionString, CancellationToken cancellationToken = default)
        {
            var region = new Region(
                          0,
                          0,
                          Console.WindowWidth,
                          Console.WindowHeight,
                          true);
            List<CurrentVersion> currentVersions = null;

            _schemaClient.SetUri(server);

            try
            {
                _sqlServerDataStore.ConnectionString = connectionString;

                // Base schema is required to run the schema migration tool.
                // This method also initializes the database if not initialized yet.
                await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(cancellationToken);

                // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
                // since the Schema job polls and upserts at the specified interval in the service.
                await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(cancellationToken);

                currentVersions = await _schemaClient.GetCurrentVersionInformationAsync(cancellationToken);
            }
            catch (SchemaManagerException ex)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
            catch (HttpRequestException)
            {
                CommandUtils.PrintError(string.Format(Resources.RequestFailedMessage, server));
                return;
            }

            var tableView = new TableView<CurrentVersion>
            {
                Items = currentVersions,
            };

            tableView.AddColumn(
               cellValue: currentVersion => currentVersion.Id,
               header: new ContentView("Version"));

            tableView.AddColumn(
                cellValue: currentVersion => currentVersion.Status,
                header: new ContentView("Status"));

            tableView.AddColumn(
                cellValue: currentVersion => string.Join(", ", currentVersion.Servers),
                header: new ContentView("Servers"));

            var consoleRenderer = new ConsoleRenderer(
                invocationContext.Console,
                mode: invocationContext.BindingContext.OutputMode(),
                resetAfterRender: true);

            using (var screen = new ScreenView(renderer: consoleRenderer))
            {
                screen.Child = tableView;
                screen.Render(region);
            }
        }
    }
}
