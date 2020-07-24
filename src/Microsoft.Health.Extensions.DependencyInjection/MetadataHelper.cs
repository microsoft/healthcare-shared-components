// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;

namespace Microsoft.Health.Extensions.DependencyInjection
{
    internal class MetadataHelper
    {
        private readonly Dictionary<Type, List<(object Metadata, Type Implementation)>> _serviceMetadata = new Dictionary<Type, List<(object Metadata, Type Implementation)>>();

        public void AddMetadataLookup(object metadata, (Type Service, Type Implementation) serviceMapping)
        {
            EnsureArg.IsNotNull(metadata, nameof(metadata));
            EnsureArg.IsNotNull(serviceMapping.Implementation, nameof(serviceMapping.Implementation));
            EnsureArg.IsNotNull(serviceMapping.Service, nameof(serviceMapping.Service));

            if (!_serviceMetadata.TryGetValue(serviceMapping.Service, out List<(object Metadata, Type Implementation)> list))
            {
                list = new List<(object Metadata, Type Implementation)>();
            }

            list.Add((metadata, serviceMapping.Implementation));
            _serviceMetadata[serviceMapping.Service] = list;
        }

        public bool TryGetMetadata(Type service, out IEnumerable<(object Metadata, Type Implementation)> metadata)
        {
            EnsureArg.IsNotNull(service, nameof(service));

            if (_serviceMetadata.TryGetValue(service, out var mapping))
            {
                metadata = mapping;
                return true;
            }

            metadata = null;
            return false;
        }
    }
}