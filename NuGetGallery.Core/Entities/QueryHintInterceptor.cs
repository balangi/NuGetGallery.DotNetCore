// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NuGetGallery;

/// <summary>
/// A global (static) interceptor for Entity Framework queries. This is used for
/// <see cref="IReadOnlyEntitiesContext.WithQueryHint(string)"/> to set a query hint for a short window of time on
/// a specific entity context. Inspired by: https://stackoverflow.com/a/45170243
/// </summary>
public sealed class QueryHintInterceptor : IDbCommandInterceptor
{
    [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "The query hint is not customer input.")]
    public InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (eventData.Context is IReadOnlyEntitiesContext entitiesContext)
        {
            var queryHint = entitiesContext.QueryHint;
            if (!string.IsNullOrEmpty(queryHint))
            {
                command.CommandText += $" OPTION ( {queryHint} )";
            }
        }

        return result;
    }
}
