// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace NuGetGallery.Areas.Admin.Models;

public interface ISupportRequestDbContext
{
    DbSet<Admin> Admins { get; set; }
    DbSet<Issue> Issues { get; set; }
    DbSet<History> Histories { get; set; }
    DbSet<IssueStatus> IssueStatus { get; set; }
    Task CommitChangesAsync();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
