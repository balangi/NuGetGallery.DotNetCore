// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Query;

namespace NuGetGallery.WebApi;

public static class QueryResultDefaults
{
    public static readonly ODataQuerySettings DefaultQuerySettings = new()
    {
        HandleNullPropagation = HandleNullPropagationOption.False,
        EnsureStableOrdering = true,
        EnableConstantParameterization = false
    };
}
