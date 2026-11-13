// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Storage;

namespace NuGetGallery;

public interface IDatabase
{
    Task<IDbContextTransaction> BeginTransactionAsync();

    Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);

    Task<int> ExecuteSqlResourceAsync(string name, params object[] parameters);
}
