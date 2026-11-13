// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NuGet.Services.Entities;
using System.Data.Common;

namespace NuGetGallery;

public class EntitiesContext : DbContext, IEntitiesContext
{
    private readonly bool _readOnly;
    private IDatabase? _databaseWrapper;

    private const string CertificatesThumbprintIndex =
        "IX_Certificates_Thumbprint";

    private const string UserSecurityPolicyUserKeyNameSubscriptionIndex =
        "IX_UserSecurityPolicy_UserKeyNameSubscription";

    /// <summary>
    /// Constructor مورد استفاده در ASP.NET Core و Dependency Injection.
    /// </summary>
    public EntitiesContext(
        DbContextOptions<EntitiesContext> options,
        bool readOnly = false)
        : base(options)
    {
        _readOnly = readOnly;
    }

    /// <summary>
    /// Constructor برای استفاده با connection string.
    /// </summary>
    public EntitiesContext(
        string connectionString,
        bool readOnly = false)
        : this(
            new DbContextOptionsBuilder<EntitiesContext>()
                .UseSqlServer(connectionString)
                .Options,
            readOnly)
    {
    }

    /// <summary>
    /// Constructor برای استفاده با DbConnection.
    /// </summary>
    public EntitiesContext(
        DbConnection connection,
        bool readOnly = false)
        : this(
            new DbContextOptionsBuilder<EntitiesContext>()
                .UseSqlServer(
                    connection!,
                    sqlServerOptionsAction: null,
                    contextOwnsConnection: true)
                .Options,
            readOnly)
    {
    }

    public bool ReadOnly => _readOnly;

    public string? QueryHint { get; private set; }

    public DbSet<Package> Packages { get; set; } = null!;
    public DbSet<PackageDeprecation> Deprecations { get; set; } = null!;
    public DbSet<PackageRegistration> PackageRegistrations { get; set; } = null!;
    public DbSet<PackageDependency> PackageDependencies { get; set; } = null!;
    public DbSet<PackageFramework> PackageFrameworks { get; set; } = null!;
    public DbSet<Credential> Credentials { get; set; } = null!;
    public DbSet<Scope> Scopes { get; set; } = null!;
    public DbSet<UserSecurityPolicy> UserSecurityPolicies { get; set; } = null!;
    public DbSet<ReservedNamespace> ReservedNamespaces { get; set; } = null!;
    public DbSet<Certificate> Certificates { get; set; } = null!;
    public DbSet<UserCertificate> UserCertificates { get; set; } = null!;
    public DbSet<SymbolPackage> SymbolPackages { get; set; } = null!;
    public DbSet<StagedPackage> StagedPackages { get; set; } = null!;
    public DbSet<StagedSymbolPackage> StagedSymbolPackages { get; set; } = null!;
    public DbSet<StagedPackageIdentity> StagedPackageIdentities { get; set; } = null!;
    public DbSet<StagingGroup> StagingGroups { get; set; } = null!;
    public DbSet<StagingBlobCleanup> StagingBlobCleanups { get; set; } = null!;
    public DbSet<PackageVulnerability> Vulnerabilities { get; set; } = null!;
    public DbSet<VulnerablePackageVersionRange> VulnerableRanges { get; set; } = null!;
    public DbSet<PackageRename> PackageRenames { get; set; } = null!;
    public DbSet<FederatedCredentialPolicy> FederatedCredentialPolicies { get; set; } = null!;
    public DbSet<FederatedCredential> FederatedCredentials { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public bool HasChanges => ChangeTracker.HasChanges();

    DbSet<T> IReadOnlyEntitiesContext.Set<T>()
    {
        return Set<T>();
    }

    public override int SaveChanges()
    {
        ThrowIfReadOnly();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ThrowIfReadOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfReadOnly();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ThrowIfReadOnly();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void ThrowIfReadOnly()
    {
        if (ReadOnly)
        {
            throw new ReadOnlyModeException(
                "Save changes unavailable: the gallery is currently in read only mode, with limited service. Please try again later.");
        }
    }

    public void DeleteOnCommit<T>(T entity)
        where T : class
    {
        Set<T>().Remove(entity);
    }

    public void SetCommandTimeout(int? seconds)
    {
        Database.SetCommandTimeout(seconds);
    }

    public IDatabase GetDatabase()
    {
        return _databaseWrapper ??= new DatabaseWrapper(this);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCredential(modelBuilder);
        ConfigureScope(modelBuilder);
        ConfigureUserSecurityPolicy(modelBuilder);
        ConfigureCertificate(modelBuilder);
        ConfigureUserCertificate(modelBuilder);
        ConfigurePackageDeprecation(modelBuilder);
    }

    private static void ConfigureCredential(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credential>(entity =>
        {
            entity.HasKey(c => c.Key);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Credentials)
                  .HasForeignKey(c => c.UserKey)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureScope(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Scope>(entity =>
        {
            entity.HasKey(s => s.Key);

            entity.HasOne(s => s.Owner)
                  .WithMany()
                  .HasForeignKey(s => s.OwnerKey)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(s => s.Credential)
                  .WithMany(c => c.Scopes)
                  .HasForeignKey(s => s.CredentialKey)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserSecurityPolicy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSecurityPolicy>(entity =>
        {
            entity.HasKey(p => p.Key);

            entity.Property(p => p.UserKey)
                  .IsRequired();

            entity.Property(p => p.Name)
                  .HasMaxLength(256)
                  .IsRequired();

            entity.Property(p => p.Subscription)
                  .HasMaxLength(256)
                  .IsRequired();

            entity.HasIndex(p => new
            {
                p.UserKey,
                p.Name,
                p.Subscription
            })
            .HasDatabaseName(
                "IX_UserSecurityPolicy_UserKeyNameSubscription")
            .IsUnique();

            entity.HasOne(p => p.User)
                  .WithMany(u => u.SecurityPolicies)
                  .HasForeignKey(p => p.UserKey)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCertificate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(c => c.Key);

            entity.Property(c => c.Thumbprint)
                  .HasMaxLength(256)
                  .HasColumnType("varchar(256)")
                  .IsRequired();

            entity.HasIndex(c => c.Thumbprint)
                  .HasDatabaseName("IX_Certificates_Thumbprint")
                  .IsUnique();

            entity.Property(c => c.Sha1Thumbprint)
                  .HasMaxLength(40)
                  .HasColumnType("varchar(40)")
                  .IsRequired();

            entity.Property(c => c.Expiration)
                  .HasColumnType("datetime2")
                  .IsRequired(false);
        });
    }

    private static void ConfigureUserCertificate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserCertificate>(entity =>
        {
            entity.HasKey(uc => uc.Key);

            entity.HasIndex(uc => new
            {
                uc.CertificateKey,
                uc.UserKey
            })
            .HasDatabaseName("IX_UserCertificates_CertificateKeyUserKey")
            .IsUnique();

            entity.HasOne(uc => uc.User)
                  .WithMany(u => u.UserCertificates)
                  .HasForeignKey(uc => uc.UserKey)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Certificate)
                  .WithMany(c => c.UserCertificates)
                  .HasForeignKey(uc => uc.CertificateKey)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePackageDeprecation(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageDeprecation>(entity =>
        {
            entity.HasKey(d => d.Key);

            entity.HasIndex(d => d.PackageKey)
                  .IsUnique();

            entity.Property(d => d.DeprecatedOn)
                  .ValueGeneratedOnAddOrUpdate();

            entity.HasOne(d => d.Package)
                  .WithMany(p => p.Deprecations)
                  .HasForeignKey(d => d.PackageKey)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.DeprecatedByUser)
                  .WithMany()
                  .HasForeignKey(d => d.DeprecatedByUserKey)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }

    public Task<int> SaveChangesAsync()
    {
        ThrowIfReadOnly();
        return base.SaveChangesAsync();
    }

    public IDisposable WithQueryHint(string queryHint)
    {
        ArgumentNullException.ThrowIfNull(queryHint);

        var previousQueryHint = QueryHint;
        QueryHint = queryHint;

        return new QueryHintScope(this, previousQueryHint);
    }

    private sealed class QueryHintScope : IDisposable
    {
        private readonly EntitiesContext _context;
        private readonly string? _previousQueryHint;
        private bool _disposed;

        public QueryHintScope(
            EntitiesContext context,
            string? previousQueryHint)
        {
            _context = context;
            _previousQueryHint = previousQueryHint;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _context.QueryHint = _previousQueryHint;
            _disposed = true;
        }
    }
}
