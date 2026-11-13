// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NuGetGallery;

public class DatabaseWrapper : IDatabase
{
    private readonly DbContext _dbContext;

    public DatabaseWrapper(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _dbContext.Database.BeginTransactionAsync();
    }

    public Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
    {
        return _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public async Task<int> ExecuteSqlResourceAsync(string name, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Resource name must not be null or empty.", nameof(name));
        }

        var assembly = typeof(DatabaseWrapper).Assembly;

        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new ArgumentException($"SQL resource '{name}' was not found.", nameof(name));

        using var reader = new StreamReader(stream);
        var sqlCommand = await reader.ReadToEndAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(sqlCommand))
        {
            throw new ArgumentException($"SQL resource '{name}' is empty.", nameof(name));
        }

        return await ExecuteSqlRawAsync(sqlCommand, parameters).ConfigureAwait(false);
    }
}
