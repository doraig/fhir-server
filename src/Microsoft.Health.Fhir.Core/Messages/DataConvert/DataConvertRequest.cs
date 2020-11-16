﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using MediatR;
using Microsoft.Health.Fhir.Core.Features.Operations.DataConvert.Models;

namespace Microsoft.Health.Fhir.Core.Messages.DataConvert
{
    /// <summary>
    /// Request for data conversion, currently supports Hl7v2 to FHIR conversion only.
    /// </summary>
    public class DataConvertRequest : IRequest<DataConvertResponse>
    {
        public DataConvertRequest(string inputData, DataConvertInputDataType inputDataType, string registryServer, string templateCollectionReference, string entryPointTemplate)
        {
            EnsureArg.IsNotNullOrEmpty(inputData, nameof(inputData));
            EnsureArg.IsNotNull<DataConvertInputDataType>(inputDataType, nameof(inputDataType));
            EnsureArg.IsNotNull(registryServer, nameof(registryServer));
            EnsureArg.IsNotNull(templateCollectionReference, nameof(templateCollectionReference));
            EnsureArg.IsNotNullOrEmpty(entryPointTemplate, nameof(entryPointTemplate));

            InputData = inputData;
            InputDataType = inputDataType;
            RegistryServer = registryServer;
            TemplateCollectionReference = templateCollectionReference;
            EntryPointTemplate = entryPointTemplate;
        }

        /// <summary>
        /// Input data in string format.
        /// </summary>
        public string InputData { get; }

        /// <summary>
        /// Data type of input data, currently accepts Hl7v2.
        /// </summary>
        public DataConvertInputDataType InputDataType { get; }

        /// <summary>
        /// Container Registry Server extracted from template reference.
        /// </summary>
        public string RegistryServer { get; }

        /// <summary>
        /// Reference for template collection.
        /// The format is "<registryServer>/<imageName>:<imageTag>" for template collection stored in container registries.
        /// Also supports image digest as reference. Will use 'latest' if no tag or digest present.
        /// </summary>
        public string TemplateCollectionReference { get; }

        /// <summary>
        /// Tells the convert engine which entry point template is used for this conversion call since we have a bunch of templates for different data types.
        /// </summary>
        public string EntryPointTemplate { get; }
    }
}
