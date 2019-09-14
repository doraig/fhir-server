﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using MediatR;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features.Conformance;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Messages.Create;
using Microsoft.Health.Fhir.Core.Messages.Upsert;
using Microsoft.Health.Fhir.Core.Notifications;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.Core.Features.Resources.Create
{
    /// <summary>
    /// Handles create resource
    /// </summary>
    public partial class CreateResourceHandler : BaseResourceHandler, IRequestHandler<CreateResourceRequest, UpsertResourceResponse>
    {
        private readonly IMediator _mediator;
        private readonly ResourceModifierEngine _resourceModifierEngine;

        public CreateResourceHandler(
            IFhirDataStore fhirDataStore,
            Lazy<IConformanceProvider> conformanceProvider,
            IResourceWrapperFactory resourceWrapperFactory,
            IMediator mediator,
            ResourceModifierEngine resourceModifierEngine)
            : base(fhirDataStore, conformanceProvider, resourceWrapperFactory)
        {
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(resourceModifierEngine, nameof(resourceModifierEngine));

            _mediator = mediator;

            _resourceModifierEngine = resourceModifierEngine;
        }

        public async Task<UpsertResourceResponse> Handle(CreateResourceRequest message, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            var resource = message.Resource.Instance.ToPoco<Resource>();

            // If an Id is supplied on create it should be removed/ignored
            resource.Id = null;

            _resourceModifierEngine.Modify(resource);

            ResourceWrapper resourceWrapper = CreateResourceWrapper(resource, deleted: false);

            bool keepHistory = await ConformanceProvider.Value.CanKeepHistory(resource.TypeName, cancellationToken);

            UpsertOutcome result = await FhirDataStore.UpsertAsync(
                resourceWrapper,
                weakETag: null,
                allowCreate: true,
                keepHistory: keepHistory,
                cancellationToken: cancellationToken);

            resource.VersionId = result.Wrapper.Version;

            await HandleVersionSpecificOperations(resource, cancellationToken);

            await _mediator.Publish(new UpsertResourceNotification(resource, "create"), cancellationToken);

            return new UpsertResourceResponse(new SaveOutcome(resource.ToResourceElement(), SaveOutcomeType.Created));
        }

        protected override void AddResourceCapability(IListedCapabilityStatement statement, string resourceType)
        {
            statement.TryAddRestInteraction(resourceType, TypeRestfulInteraction.Create);
        }
    }
}
