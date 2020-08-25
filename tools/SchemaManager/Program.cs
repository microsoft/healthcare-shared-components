// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using SchemaManager.Commands;
using SchemaManager.Model;

namespace SchemaManager
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var serverOption = new Option(
                OptionAliases.Server,
                Resources.ServerOptionDescription,
                new Argument<Uri> { Arity = ArgumentArity.ExactlyOne });
            serverOption.AddAlias(OptionAliases.ShortServer);

            var connectionStringOption = new Option(
                OptionAliases.ConnectionString,
                Resources.ConnectionStringOptionDescription,
                new Argument<string> { Arity = ArgumentArity.ExactlyOne });
            connectionStringOption.AddAlias(OptionAliases.ShortConnectionString);

            var versionOption = new Option(
                OptionAliases.Version,
                Resources.VersionOptionDescription,
                new Argument<int> { Arity = ArgumentArity.ExactlyOne });
            versionOption.AddAlias(OptionAliases.ShortVersion);

            var nextOption = new Option(
               OptionAliases.Next,
               Resources.NextOptionDescritpion,
               new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });
            nextOption.AddAlias(OptionAliases.ShortNext);

            var latestOption = new Option(
               OptionAliases.Latest,
               Resources.LatestOptionDescription,
               new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });
            latestOption.AddAlias(OptionAliases.ShortLatest);

            var forceOption = new Option(
                OptionAliases.Force,
                Resources.ForceOptionDescription,
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });
            forceOption.AddAlias(OptionAliases.ShortForce);

            var serviceOption = new Option(
                OptionAliases.Service,
                Resources.ForceOptionDescription,
                new Argument<Service> { Arity = ArgumentArity.ZeroOrOne });
            serviceOption.AddAlias(OptionAliases.ShortService);

            var rootCommand = new RootCommand();

            var currentCommand = new Command(CommandNames.Current, Resources.CurrentCommandDescription)
            {
                serverOption,
            };
            currentCommand.Handler = CommandHandler.Create<InvocationContext, Uri>(CurrentCommand.HandlerAsync);
            currentCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, serverOption, Resources.ServerRequiredValidation));

            var applyCommand = new Command(CommandNames.Apply, Resources.ApplyCommandDescription)
            {
                connectionStringOption,
                serverOption,
                versionOption,
                nextOption,
                latestOption,
                forceOption,
            };
            applyCommand.Handler = CommandHandler.Create<string, Uri, MutuallyExclusiveType, bool>(ApplyCommand.HandlerAsync);
            applyCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, connectionStringOption, Resources.ConnectionStringRequiredValidation));
            applyCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, serverOption, Resources.ServerRequiredValidation));
            applyCommand.Argument.AddValidator(symbol => Validators.MutuallyExclusiveOptionValidator.Validate(symbol, new List<Option> { versionOption, nextOption, latestOption }, Resources.MutuallyExclusiveValidation));

            var availableCommand = new Command(CommandNames.Available, Resources.AvailableCommandDescription)
            {
                serverOption,
            };
            availableCommand.Handler = CommandHandler.Create<InvocationContext, Uri>(AvailableCommand.Handler);
            availableCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, serverOption, Resources.ServerRequiredValidation));

            var baseCommand = new Command(CommandNames.Base, Resources.BaseCommandDescription)
            {
                connectionStringOption,
                serviceOption,
            };
            baseCommand.Handler = CommandHandler.Create<string>(BaseCommand.Handler);
            baseCommand.Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, connectionStringOption, Resources.ConnectionStringRequiredValidation));

            rootCommand.AddCommand(applyCommand);
            rootCommand.AddCommand(availableCommand);
            rootCommand.AddCommand(currentCommand);
            rootCommand.AddCommand(baseCommand);

            rootCommand.InvokeAsync(args).Wait();
        }
    }
}
