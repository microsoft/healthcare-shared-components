// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.MeasurementUtility;

public interface ITimed : IDisposable
{
    TimeSpan Elapsed { get; }

    ITimed Record();
}
