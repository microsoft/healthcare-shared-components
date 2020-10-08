// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SchemaManager.Exceptions;
using SchemaManager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class CurrentCommand
    {
        public static async Task HandlerAsync(InvocationContext invocationContext, Uri server, string connectionString, CancellationToken cancellationToken = default)
        {
            var region = new Region(
                          0,
                          0,
                          Console.WindowWidth,
                          Console.WindowHeight,
                          true);
            List<CurrentVersion> currentVersions = null;
            ISchemaClient schemaClient = new SchemaClient(server);

            try
            {
                // Base schema is required to run the schema migration tool.
                // This method also initializes the database if not initialized yet.
                await BaseSchemaRunner.EnsureBaseSchemaExistsAsync(connectionString, cancellationToken);

                // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
                // since the Schema job polls and upserts at the specified interval in the service.
                BaseSchemaRunner.EnsureInstanceSchemaRecordExists(connectionString);

                currentVersions = await schemaClient.GetCurrentVersionInformation();
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
