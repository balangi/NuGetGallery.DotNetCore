// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NuGet.Services.Entities;

namespace NuGetGallery;

public class ReadOnlyEntitiesContext : IReadOnlyEntitiesContext, IAsyncDisposable
{
    private readonly EntitiesContext _entitiesContext;
    private bool _disposed;

    public ReadOnlyEntitiesContext(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _entitiesContext = new EntitiesContext(connection, readOnly: true);
    }

    public ReadOnlyEntitiesContext(DbContextOptions<EntitiesContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _entitiesContext = new EntitiesContext(options, readOnly: true);
    }

    public DbSet<Package> Packages
    {
        get => _entitiesContext.Packages;
        set => _entitiesContext.Packages = value;
    }

    public string? QueryHint => _entitiesContext.QueryHint;

    public DbSet<T> Set<T>() where T : class
    {
        return _entitiesContext.Set<T>();
    }

    public void SetCommandTimeout(int? seconds)
    {
        _entitiesContext.SetCommandTimeout(seconds);
    }

    public IDisposable WithQueryHint(string queryHint)
    {
        return _entitiesContext.WithQueryHint(queryHint);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _entitiesContext.Dispose();
            }

            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            await _entitiesContext.DisposeAsync().ConfigureAwait(false);
        }
    }
}
