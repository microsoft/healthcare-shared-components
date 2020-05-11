// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace SchemaManager.Utils
{
    public static class UrlConstants
    {
        public const string Schema = "/_schema";
        public const string Versions = "/versions";
        public const string Current = Schema + Versions + "/current";
        public const string Compatibility = Schema + "/compatibility";
        public const string Availability = Schema + Versions;
        public const string Diff = Schema + Versions + "/{0}/diff";
    }
}
