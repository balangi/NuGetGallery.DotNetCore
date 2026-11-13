// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Storage;

namespace NuGetGallery;

public class DbContextTransactionWrapper : IDbContextTransaction
{
    private readonly IDbContextTransaction _transaction;

    public DbContextTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public Guid TransactionId => _transaction.TransactionId;

    public void Commit()
    {
        _transaction.Commit();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public void Rollback()
    {
        _transaction.Rollback();
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}
