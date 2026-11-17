// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace NuGetGallery.WebApi;

public static class QueryResultExtensions
{
    public static IActionResult FormattedAsCountResult<T>(this IActionResult current)
    {
        var queryResult = current as QueryResult<T>;
        if (queryResult != null)
        {
            queryResult.FormatAsCountResult = true;
            return queryResult;
        }

        return current;
    }

    public static IActionResult FormattedAsSingleResult<T>(this IActionResult current)
    {
        var queryResult = current as QueryResult<T>;
        if (queryResult != null)
        {
            queryResult.FormatAsSingleResult = true;
            return queryResult;
        }

        return current;
    }
}
