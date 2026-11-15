// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NuGetGallery.Areas.Admin.Models;

public class SupportRequestDbContextFactory : IDesignTimeDbContextFactory<SupportRequestDbContext>, IDbContextFactory<SupportRequestDbContext>
{
    public static Func<SupportRequestDbContext> SupportRequestEntitiesContextFactory;

    public SupportRequestDbContext CreateDbContext(string[] args = null)
    {
        var factory = SupportRequestEntitiesContextFactory;
        if (factory != null)
        {
            return factory();
        }

        var optionsBuilder = new DbContextOptionsBuilder<SupportRequestDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SupportRequestDb;Trusted_Connection=True;MultipleActiveResultSets=true");

        return new SupportRequestDbContext(optionsBuilder.Options);
    }

    // پیاده‌سازی صریح برای IDbContextFactory (زمان اجرای برنامه)
    SupportRequestDbContext IDbContextFactory<SupportRequestDbContext>.CreateDbContext()
    {
        return CreateDbContext(null);
    }
}

public class SupportRequestDbContext
    : DbContext, ISupportRequestDbContext
{
    public SupportRequestDbContext(DbContextOptions<SupportRequestDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Issue> Issues { get; set; }

    public virtual DbSet<IssueStatus> IssueStatus { get; set; }

    public virtual DbSet<History> Histories { get; set; }

    public async Task CommitChangesAsync()
    {
        await SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Admin>()
            .Property(e => e.GalleryUsername).IsUnicode(false);

        modelBuilder.Entity<Admin>()
            .HasMany(e => e.Issues)
            .WithOne(e => e.AssignedTo)
            .IsRequired(false)
            .HasForeignKey(e => e.AssignedToId);

        modelBuilder.Entity<Admin>()
            .HasMany(e => e.HistoryEntries)
            .WithOne()
            .IsRequired(false)
            .HasForeignKey(e => e.AssignedToId);

        modelBuilder.Entity<History>()
           .Property(e => e.Comments).IsUnicode(false);
        modelBuilder.Entity<History>()
           .Property(e => e.EditedBy).IsUnicode(false);

        modelBuilder.Entity<Issue>()
             .Property(e => e.CreatedBy).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.IssueTitle).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.Details).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.SiteRoot).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.PackageId).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.PackageVersion).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.OwnerEmail).IsUnicode(false);
        modelBuilder.Entity<Issue>()
            .Property(e => e.Reason).IsUnicode(false);

        modelBuilder.Entity<Issue>()
            .HasOne(e => e.AssignedTo)
            .WithMany(e => e.Issues)
            .HasForeignKey(e => e.AssignedToId)
            .IsRequired(false);

        modelBuilder.Entity<Issue>()
            .HasMany(e => e.HistoryEntries)
            .WithOne()
            .IsRequired()
            .HasForeignKey(e => e.IssueId);

        modelBuilder.Entity<Issue>()
            .HasOne(e => e.IssueStatus)
            .WithMany(e => e.Issues)
            .HasForeignKey(e => e.IssueStatusId)
            .IsRequired();

        modelBuilder.Entity<IssueStatus>()
             .Property(e => e.Name).IsUnicode(false);

        modelBuilder.Entity<IssueStatus>()
            .HasMany(e => e.Issues)
            .WithOne(e => e.IssueStatus)
            .HasForeignKey(e => e.IssueStatusId)
            .IsRequired();
    }
}
