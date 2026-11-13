// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using NuGet.Services.Entities;

namespace NuGetGallery;

public interface IReadOnlyEntitiesContext : IDisposable
{
    DbSet<Package> Packages { get; set; }

    DbSet<T> Set<T>()
        where T : class;

    void SetCommandTimeout(int? seconds);

    /// <summary>
    /// Sets the query hint for queries to the provided value until
    /// the returned disposable is disposed.
    /// </summary>
    /// <param name="queryHint">The query hint.</param>
    /// <returns>
    /// A disposable that restores the previous query hint.
    /// </returns>
    IDisposable WithQueryHint(string queryHint);

    /// <summary>
    /// The current query hint to use for queries.
    /// </summary>
    string? QueryHint { get; }
}
