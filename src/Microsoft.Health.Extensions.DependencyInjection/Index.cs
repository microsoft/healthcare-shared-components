// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnsureThat;

namespace Microsoft.Health.Extensions.DependencyInjection
{
    internal sealed class Index<TServiceType> : IIndex<TServiceType>
    {
        private readonly Dictionary<object, Lazy<TServiceType>> _services;

        public Index(IServiceProvider serviceProvider, MetadataHelper metadataHelper)
        {
            EnsureArg.IsNotNull(serviceProvider, nameof(TServiceType));
            EnsureArg.IsNotNull(metadataHelper, nameof(metadataHelper));

            if (metadataHelper.TryGetMetadata(typeof(TServiceType), out var mappings))
            {
                _services = mappings
                    .Select(x =>
                    {
                        Type implementation = x.Implementation;
                        var lazyResolve = new Lazy<TServiceType>(() => (TServiceType)serviceProvider.GetService(implementation));
                        return new KeyValuePair<object, Lazy<TServiceType>>(x.Metadata, lazyResolve);
                    })
                    .ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                _services = new Dictionary<object, Lazy<TServiceType>>();
            }
        }

        public TServiceType this[object index]
        {
            get
            {
                EnsureArg.IsNotNull(index, nameof(index));
                return _services[index].Value;
            }
        }

        public bool TryGetValue(object metadata, out TServiceType value)
        {
            EnsureArg.IsNotNull(metadata, nameof(metadata));

            if (_services.TryGetValue(metadata, out Lazy<TServiceType> service))
            {
                value = service.Value;
                return true;
            }

            value = default;
            return false;
        }

        public IEnumerator<KeyValuePair<object, Lazy<TServiceType>>> GetEnumerator()
        {
            return _services.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}