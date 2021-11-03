// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.CommandLine;

namespace Microsoft.Health.SchemaManager
{
    public static class CommandOptions
    {
        public static Option ServerOption()
        {
            var serverOption = new Option(
                OptionAliases.Server,
                Resources.ServerOptionDescription,
                new Argument<Uri> { Arity = ArgumentArity.ExactlyOne });
            serverOption.AddAlias(OptionAliases.ShortServer);

            return serverOption;
        }

        public static Option ConnectionStringOption()
        {
            var connectionStringOption = new Option(
                OptionAliases.ConnectionString,
                Resources.ConnectionStringOptionDescription,
                new Argument<string> { Arity = ArgumentArity.ExactlyOne });
            connectionStringOption.AddAlias(OptionAliases.ShortConnectionString);

            return connectionStringOption;
        }

        public static Option VersionOption()
        {
            var versionOption = new Option(
                OptionAliases.Version,
                Resources.VersionOptionDescription,
                new Argument<int> { Arity = ArgumentArity.ExactlyOne });
            versionOption.AddAlias(OptionAliases.ShortVersion);

            return versionOption;
        }

        public static Option NextOption()
        {
            var nextOption = new Option(
               OptionAliases.Next,
               Resources.NextOptionDescritpion,
               new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });
            nextOption.AddAlias(OptionAliases.ShortNext);

            return nextOption;
        }

        public static Option LatestOption()
        {
            var latestOption = new Option(
               OptionAliases.Latest,
               Resources.LatestOptionDescription,
               new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });
            latestOption.AddAlias(OptionAliases.ShortLatest);

            return latestOption;
        }

        public static Option ForceOption()
        {
            var forceOption = new Option(
                OptionAliases.Force,
                Resources.ForceOptionDescription,
                new Argument<bool> { Arity = ArgumentArity.ZeroOrOne });
            forceOption.AddAlias(OptionAliases.ShortForce);

            return forceOption;
        }
    }
}
