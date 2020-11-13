﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Fhir.Core.Features.Definition.BundleWrappers;
using Microsoft.Health.Fhir.Core.Features.Search;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Definition
{
    /// <summary>
    /// Provides mechanism to access search parameter definition.
    /// </summary>
    public class SearchParameterDefinitionManager : ISearchParameterDefinitionManager, IHostedService
    {
        private readonly IModelInfoProvider _modelInfoProvider;
        private bool _started;
        private ConcurrentDictionary<string, string> _resourceTypeSearchParameterHashMap;

        public SearchParameterDefinitionManager(IModelInfoProvider modelInfoProvider)
        {
            EnsureArg.IsNotNull(modelInfoProvider, nameof(modelInfoProvider));

            _modelInfoProvider = modelInfoProvider;
            _resourceTypeSearchParameterHashMap = new ConcurrentDictionary<string, string>();
            TypeLookup = new Dictionary<string, IDictionary<string, SearchParameterInfo>>();
            UrlLookup = new Dictionary<Uri, SearchParameterInfo>();
        }

        internal IDictionary<Uri, SearchParameterInfo> UrlLookup { get; set; }

        internal IDictionary<string, IDictionary<string, SearchParameterInfo>> TypeLookup { get; }

        public IEnumerable<SearchParameterInfo> AllSearchParameters => UrlLookup.Values;

        public IReadOnlyDictionary<string, string> SearchParameterHashMap
        {
            get { return new ReadOnlyDictionary<string, string>(_resourceTypeSearchParameterHashMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)); }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // This method is idempotent because dependent Start methods are not guaranteed to be executed in order.
            if (!_started)
            {
                var bundle = SearchParameterDefinitionBuilder.ReadEmbeddedSearchParameters("search-parameters.json", _modelInfoProvider);

                SearchParameterDefinitionBuilder.Build(
                    bundle,
                    UrlLookup,
                    TypeLookup,
                    _modelInfoProvider);

                List<string> list = UrlLookup.Values.Where(p => p.Type == ValueSets.SearchParamType.Composite).Select(p => string.Join("|", p.Component.Select(c => UrlLookup[c.DefinitionUrl].Type))).Distinct().ToList();

                CalculateSearchParameterHashAsync();

                _started = true;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public IEnumerable<SearchParameterInfo> GetSearchParameters(string resourceType)
        {
            if (TypeLookup.TryGetValue(resourceType, out IDictionary<string, SearchParameterInfo> value))
            {
                return value.Values;
            }

            throw new ResourceNotSupportedException(resourceType);
        }

        public SearchParameterInfo GetSearchParameter(string resourceType, string name)
        {
            if (TypeLookup.TryGetValue(resourceType, out IDictionary<string, SearchParameterInfo> lookup) &&
                lookup.TryGetValue(name, out SearchParameterInfo searchParameter))
            {
                return searchParameter;
            }

            throw new SearchParameterNotSupportedException(resourceType, name);
        }

        public bool TryGetSearchParameter(string resourceType, string name, out SearchParameterInfo searchParameter)
        {
            searchParameter = null;

            return TypeLookup.TryGetValue(resourceType, out IDictionary<string, SearchParameterInfo> searchParameters) &&
                searchParameters.TryGetValue(name, out searchParameter);
        }

        public SearchParameterInfo GetSearchParameter(Uri definitionUri)
        {
            if (UrlLookup.TryGetValue(definitionUri, out SearchParameterInfo value))
            {
                return value;
            }

            throw new SearchParameterNotSupportedException(definitionUri);
        }

        public ValueSets.SearchParamType GetSearchParameterType(SearchParameterInfo searchParameter, int? componentIndex)
        {
            if (componentIndex == null)
            {
                return searchParameter.Type;
            }

            SearchParameterComponentInfo component = searchParameter.Component[componentIndex.Value];
            SearchParameterInfo componentSearchParameter = GetSearchParameter(component.DefinitionUrl);

            return componentSearchParameter.Type;
        }

        public string GetSearchParameterHashForResourceType(string resourceType)
        {
            EnsureArg.IsNotNullOrWhiteSpace(resourceType, nameof(resourceType));

            if (_resourceTypeSearchParameterHashMap.TryGetValue(resourceType, out string hash))
            {
                return hash;
            }

            return null;
        }

        public void AddNewSearchParameters(BundleWrapper searchParamBundle)
        {
            SearchParameterDefinitionBuilder.Build(
                searchParamBundle,
                UrlLookup,
                TypeLookup,
                _modelInfoProvider);

            CalculateSearchParameterHashAsync();
        }

        private void CalculateSearchParameterHashAsync()
        {
            foreach (KeyValuePair<string, IDictionary<string, SearchParameterInfo>> kvp in TypeLookup)
            {
                string searchParamHash = SearchHelperUtilities.CalculateSearchParameterHash(kvp.Value.Values);
                _resourceTypeSearchParameterHashMap.AddOrUpdate(
                    kvp.Key,
                    searchParamHash,
                    (resourceType, existingValue) => searchParamHash);
            }
        }
    }
}
