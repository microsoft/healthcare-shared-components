// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Exceptions;

namespace Microsoft.Health.Core.Configs;

public class AuditConfiguration
{
    private string _customAuditHeaderPrefix;

    public AuditConfiguration()
    {
    }

    public AuditConfiguration(string customAuditHeaderPrefix)
    {
        CustomAuditHeaderPrefix = customAuditHeaderPrefix;
    }

    public string CustomAuditHeaderPrefix
    {
        get
        {
            return _customAuditHeaderPrefix;
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidDefinitionException(Resources.CustomHeaderPrefixCannotBeEmpty);
            }

            _customAuditHeaderPrefix = value;
        }
    }
}
