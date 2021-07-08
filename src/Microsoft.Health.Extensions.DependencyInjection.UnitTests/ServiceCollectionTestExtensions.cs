// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.Extensions.DependencyInjection.UnitTests
{
    public static class ServiceCollectionTestExtensions
    {
        public static IEnumerable<ServiceDescriptor> NonSystemTypes(this IServiceCollection serviceCollection)
        {
            return serviceCollection.Where(x => !(x.ImplementationInstance is MetadataHelper) &&
                                                x.ImplementationType != typeof(Index<>));
        }
    }
}