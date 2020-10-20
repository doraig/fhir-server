﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Fhir.Core.Configs
{
    public class OperationsConfiguration
    {
        public ExportJobConfiguration Export { get; set; } = new ExportJobConfiguration();

        public ReindexJobConfiguration Reindex { get; set; } = new ReindexJobConfiguration();

        public DataConvertConfiguration Convert { get; set; } = new DataConvertConfiguration();
    }
}
