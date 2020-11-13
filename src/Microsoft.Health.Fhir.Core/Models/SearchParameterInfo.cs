﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.Health.Fhir.ValueSets;

namespace Microsoft.Health.Fhir.Core.Models
{
    [DebuggerDisplay("{Name}, Type: {Type}")]
    public class SearchParameterInfo : IEquatable<SearchParameterInfo>
    {
        public SearchParameterInfo(
            string name,
            string searchParamType,
            Uri url = null,
            IReadOnlyList<SearchParameterComponentInfo> components = null,
            string expression = null,
            IReadOnlyCollection<string> targetResourceTypes = null,
            IReadOnlyCollection<string> baseResourceTypes = null,
            string description = null)
            : this(
                name,
                Enum.Parse<SearchParamType>(searchParamType),
                url,
                components,
                expression,
                targetResourceTypes,
                baseResourceTypes,
                description)
        {
        }

        public SearchParameterInfo(
            string name,
            SearchParamType searchParamType,
            Uri url = null,
            IReadOnlyList<SearchParameterComponentInfo> components = null,
            string expression = null,
            IReadOnlyCollection<string> targetResourceTypes = null,
            IReadOnlyCollection<string> baseResourceTypes = null,
            string description = null)
            : this(name)
        {
            Url = url;
            Type = searchParamType;
            Component = components;
            Expression = expression;
            TargetResourceTypes = targetResourceTypes;
            BaseResourceTypes = baseResourceTypes;
            Description = description;
        }

        public SearchParameterInfo(string name)
        {
            EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

            Name = name;
        }

        public string Name { get; }

        public string Code { get; }

        public string Description { get; set; }

        public string Expression { get; }

        public IReadOnlyCollection<string> TargetResourceTypes { get; } = Array.Empty<string>();

        public IReadOnlyCollection<string> BaseResourceTypes { get; } = Array.Empty<string>();

        public Uri Url { get; }

        public SearchParamType Type { get; }

        /// <summary>
        /// Returns true if this parameter is enabled for searches
        /// </summary>
        public bool IsSearchable { get; set; } = true;

        /// <summary>
        /// Returns true if the system has the capability for indexing and searching for this parameter
        /// </summary>
        public bool IsSupported { get; set; } = true;

        /// <summary>
        /// Returns true if the search parameter resolves to more than one type (FhirString, FhirUri, etc...)
        /// but not all types are able to be indexed / searched
        /// </summary>
        public bool IsPartiallySupported { get; set; }

        public IReadOnlyList<SearchParameterComponentInfo> Component { get; }

        public bool Equals([AllowNull] SearchParameterInfo other)
        {
            if (other == null)
            {
                return false;
            }

            if (Url != other.Url)
            {
                return false;
            }

            if (Url == null)
            {
                if (!Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) ||
                    Type != other.Type ||
                    Expression != other.Expression)
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SearchParameterInfo);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Url?.GetHashCode(),
                Name?.GetHashCode(StringComparison.OrdinalIgnoreCase),
                Type.GetHashCode(),
                Expression?.GetHashCode(StringComparison.OrdinalIgnoreCase));
        }
    }
}
