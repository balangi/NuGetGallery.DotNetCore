// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using NuGet.Services.Validation.Entities;

namespace NuGet.Services.Validation;

/// <summary>
/// The Entity Framework Core database context for validation entities.
/// </summary>
public class ValidationEntitiesContext : DbContext, IValidationEntitiesContext
{
    private const int MaximumThumbprintLength = 256;
    private const int MaximumPackageIdLength = 128;
    private const int MaximumPackageVersionLength = 64;
    private const int MaximumPackageContentTypeLength = 128;
    private const int MaximumValidationResultTypeLength = 128;

    private const string ThumbprintColumnType = "varchar";

    private const string SignatureSchema = "signature";
    private const string ScanSchema = "scan";

    private const string ValidatorStatusesTable = "ValidatorStatuses";
    private const string PackageSigningStatesTable = "PackageSigningStates";
    private const string PackageSignaturesTable = "PackageSignatures";
    private const string TrustedTimestampsTable = "TrustedTimestamps";
    private const string EndCertificatesTable = "EndCertificates";
    private const string ParentCertificatesTable = "ParentCertificates";
    private const string CertificateChainLinksTable = "CertificateChainLinks";
    private const string EndCertificateValidationsTable = "EndCertificateValidations";
    private const string PackageCompatibilityIssuesTable = "PackageCompatibilityIssues";
    private const string ScanOperationStatesTable = "ScanOperationStates";

    public DbSet<PackageValidationSet> PackageValidationSets { get; set; } = null!;
    public DbSet<PackageValidation> PackageValidations { get; set; } = null!;
    public DbSet<PackageValidationResult> PackageValidationResults { get; set; } = null!;
    public DbSet<PackageValidationIssue> PackageValidationIssues { get; set; } = null!;
    public DbSet<ValidatorStatus> ValidatorStatuses { get; set; } = null!;
    public DbSet<PackageSigningState> PackageSigningStates { get; set; } = null!;
    public DbSet<PackageSignature> PackageSignatures { get; set; } = null!;
    public DbSet<TrustedTimestamp> TrustedTimestamps { get; set; } = null!;
    public DbSet<EndCertificate> EndCertificates { get; set; } = null!;
    public DbSet<EndCertificateValidation> CertificateValidations { get; set; } = null!;
    public DbSet<ParentCertificate> ParentCertificates { get; set; } = null!;
    public DbSet<CertificateChainLink> CertificateChainLinks { get; set; } = null!;
    public DbSet<PackageCompatibilityIssue> PackageCompatibilityIssues { get; set; } = null!;
    public DbSet<ScanOperationState> ScanOperationStates { get; set; } = null!;
    public DbSet<PackageRevalidation> PackageRevalidations { get; set; } = null!;
    public DbSet<SymbolsServerRequest> SymbolsServerRequests { get; set; } = null!;
    public DbSet<ContentScanOperationState> ContentScanOperationState { get; set; } = null!;

    public ValidationEntitiesContext(DbContextOptions<ValidationEntitiesContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PackageValidationSet>(entity =>
        {
            entity.HasKey(pvs => pvs.Key);

            entity.HasIndex(pvs => pvs.ValidationTrackingId)
                  .IsUnique();

            entity.HasIndex(pvs => pvs.PackageKey);

            entity.HasIndex(pvs => new { pvs.PackageId, pvs.PackageNormalizedVersion });

            entity.Property(pvs => pvs.PackageId)
                  .HasMaxLength(MaximumPackageIdLength);

            entity.Property(pvs => pvs.PackageNormalizedVersion)
                  .HasMaxLength(MaximumPackageVersionLength);

            entity.Property(pvs => pvs.PackageContentType)
                  .HasMaxLength(MaximumPackageContentTypeLength);

            entity.Property(pvs => pvs.RowVersion)
                  .IsRowVersion();

            entity.Property(pvs => pvs.Created)
                  .HasColumnType("datetime2");

            entity.Property(pvs => pvs.Updated)
                  .HasColumnType("datetime2");

            entity.Property(pvs => pvs.Expiration)
                  .HasColumnType("datetime2");

            entity.HasMany(v => v.PackageValidationResults)
                  .WithOne(r => r.PackageValidationSet)
                  .HasForeignKey(r => r.PackageValidationSetKey)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackageValidation>(entity =>
        {
            entity.HasKey(pv => pv.Key);
            entity.Property(pv => pv.Key).ValueGeneratedOnAdd();

            entity.Property(pv => pv.Type)
                  .HasMaxLength(255)
                  .HasColumnType("varchar(255)")
                  .IsRequired();

            entity.Property(pv => pv.Started)
                  .HasColumnType("datetime2");

            entity.Property(pv => pv.ValidationStatusTimestamp)
                  .HasColumnType("datetime2");

            entity.Property(pv => pv.RowVersion)
                  .IsRowVersion();
        });

        modelBuilder.Entity<PackageValidationResult>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(pv => pv.Key).ValueGeneratedOnAdd();

            entity.Property(pv => pv.Type)
                  .HasMaxLength(MaximumValidationResultTypeLength)
                  .IsRequired();

            entity.Property(pv => pv.Data)
                  .IsRequired();

            entity.HasOne(r => r.PackageValidation)
                  .WithMany()
                  .IsRequired(false);
        });

        modelBuilder.Entity<PackageValidationIssue>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(pv => pv.Key).ValueGeneratedOnAdd();
            entity.Property(pv => pv.Data).IsRequired();
        });

        modelBuilder.Entity<PackageCompatibilityIssue>(entity =>
        {
            entity.ToTable(PackageCompatibilityIssuesTable);
            entity.HasKey(e => e.Key);
            entity.Property(pv => pv.Key).ValueGeneratedOnAdd();
            entity.Property(pv => pv.Message).IsRequired();
            entity.Property(pv => pv.ClientIssueCode).IsRequired();
        });

        modelBuilder.Entity<ValidatorStatus>(entity =>
        {
            entity.ToTable(ValidatorStatusesTable);
            entity.HasKey(s => s.ValidationId);
            entity.Property(s => s.ValidationId).ValueGeneratedNever();

            entity.HasIndex(s => s.PackageKey);
            entity.Property(s => s.ValidatorName).IsRequired();
            entity.Property(r => r.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<ValidatorIssue>(entity =>
        {
            entity.HasKey(vi => vi.Key);
            entity.Property(vi => vi.Key).ValueGeneratedOnAdd();
            entity.Property(vi => vi.Data).IsRequired();
        });

        RegisterPackageSigningEntities(modelBuilder);
        RegisterScanningEntities(modelBuilder);
        RegisterRevalidationEntities(modelBuilder);
        RegisterSymbolEntities(modelBuilder);
        RegisterCvsScanEntities(modelBuilder);
    }

    private void RegisterPackageSigningEntities(ModelBuilder modelBuilder)
    {
        ConfigureBatchIdProperty(modelBuilder);

        modelBuilder.Entity<PackageSigningState>(entity =>
        {
            entity.ToTable(PackageSigningStatesTable, SignatureSchema);
            entity.HasKey(p => p.PackageKey);
            entity.Property(p => p.PackageKey).ValueGeneratedNever();

            entity.Property(p => p.PackageId)
                  .HasMaxLength(MaximumPackageIdLength)
                  .IsRequired();

            entity.Property(p => p.PackageNormalizedVersion)
                  .HasMaxLength(MaximumPackageVersionLength)
                  .IsRequired();

            entity.HasIndex(p => new { p.PackageId, p.PackageNormalizedVersion });

            entity.HasMany(p => p.PackageSignatures)
                  .WithOne(s => s.PackageSigningState)
                  .HasForeignKey(s => s.PackageKey)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PackageSignature>(entity =>
        {
            entity.ToTable(PackageSignaturesTable, SignatureSchema);
            entity.HasKey(s => s.Key);
            entity.Property(s => s.Key).ValueGeneratedOnAdd();

            entity.HasIndex(s => new { s.PackageKey, s.Type })
                  .IsUnique();

            entity.HasIndex(s => new { s.Type, s.Status });
            entity.HasIndex(s => s.EndCertificateKey);
            entity.HasIndex(s => s.Status);

            entity.Property(s => s.CreatedAt)
                  .HasColumnType("datetime2");

            entity.Property(c => c.RowVersion)
                  .IsRowVersion();

            entity.HasMany(s => s.TrustedTimestamps)
                  .WithOne(t => t.PackageSignature)
                  .HasForeignKey(t => t.PackageSignatureKey)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.EndCertificate)
                  .WithMany(c => c.PackageSignatures)
                  .HasForeignKey(s => s.EndCertificateKey)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrustedTimestamp>(entity =>
        {
            entity.ToTable(TrustedTimestampsTable, SignatureSchema);
            entity.HasKey(t => t.Key);

            entity.Property(t => t.Value)
                  .HasColumnType("datetime2");

            entity.HasIndex(t => t.PackageSignatureKey)
                  .IsUnique();

            entity.HasOne(s => s.EndCertificate)
                  .WithMany(c => c.TrustedTimestamps)
                  .HasForeignKey(s => s.EndCertificateKey)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EndCertificate>(entity =>
        {
            entity.ToTable(EndCertificatesTable, SignatureSchema);
            entity.HasKey(c => c.Key);

            entity.Property(c => c.Thumbprint)
                  .HasMaxLength(MaximumThumbprintLength)
                  .HasColumnType($"{ThumbprintColumnType}({MaximumThumbprintLength})")
                  .IsRequired();

            entity.HasIndex(c => c.Thumbprint)
                  .IsUnique();

            entity.Property(c => c.StatusUpdateTime).HasColumnType("datetime2");
            entity.Property(c => c.NextStatusUpdateTime).HasColumnType("datetime2");
            entity.Property(c => c.LastVerificationTime).HasColumnType("datetime2");
            entity.Property(c => c.RevocationTime).HasColumnType("datetime2");
            entity.Property(c => c.RowVersion).IsRowVersion();

            entity.HasMany(c => c.Validations)
                  .WithOne(v => v.EndCertificate)
                  .HasForeignKey(v => v.EndCertificateKey)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.CertificateChainLinks)
                  .WithOne(v => v.EndCertificate)
                  .HasForeignKey(v => v.EndCertificateKey)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CertificateChainLink>(entity =>
        {
            entity.ToTable(CertificateChainLinksTable, SignatureSchema);
            entity.HasKey(t => t.Key);

            entity.HasIndex(v => new { v.EndCertificateKey, v.ParentCertificateKey })
                  .IsUnique();
        });

        modelBuilder.Entity<ParentCertificate>(entity =>
        {
            entity.ToTable(ParentCertificatesTable, SignatureSchema);
            entity.HasKey(c => c.Key);

            entity.Property(c => c.Thumbprint)
                  .HasMaxLength(MaximumThumbprintLength)
                  .HasColumnType($"{ThumbprintColumnType}({MaximumThumbprintLength})")
                  .IsRequired();

            entity.HasIndex(c => c.Thumbprint)
                  .IsUnique();

            entity.HasMany(v => v.CertificateChainLinks)
                  .WithOne(v => v.ParentCertificate)
                  .HasForeignKey(v => v.ParentCertificateKey)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EndCertificateValidation>(entity =>
        {
            entity.ToTable(EndCertificateValidationsTable, SignatureSchema);
            entity.HasKey(v => v.Key);

            entity.HasIndex(v => v.ValidationId);
            entity.HasIndex(v => new { v.EndCertificateKey, v.ValidationId });
        });
    }

    private void RegisterScanningEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScanOperationState>(entity =>
        {
            entity.ToTable(ScanOperationStatesTable, ScanSchema);
            entity.HasKey(p => p.Key);

            entity.HasIndex(s => new { s.PackageValidationKey, s.OperationType, s.AttemptIndex })
                  .IsUnique();

            entity.HasIndex(s => new { s.ScanState, s.CreatedAt });

            entity.Property(s => s.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(s => s.StartedAt).HasColumnType("datetime2");
            entity.Property(s => s.FinishedAt).HasColumnType("datetime2");
            entity.Property(s => s.ResultUrl).HasMaxLength(2048);
            entity.Property(s => s.OperationId).HasMaxLength(64);
            entity.Property(pvs => pvs.RowVersion).IsRowVersion();
        });
    }

    private void RegisterRevalidationEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PackageRevalidation>(entity =>
        {
            entity.HasKey(r => r.Key);

            entity.Property(r => r.PackageId)
                  .HasMaxLength(MaximumPackageIdLength)
                  .IsRequired();

            entity.Property(r => r.PackageNormalizedVersion)
                  .HasMaxLength(MaximumPackageVersionLength)
                  .IsRequired();

            entity.HasIndex(r => new { r.PackageId, r.PackageNormalizedVersion });
            entity.HasIndex(r => new { r.Enqueued, r.Completed });
            entity.HasIndex(r => r.ValidationTrackingId).IsUnique();

            entity.Property(r => r.Enqueued).HasColumnType("datetime2");
            entity.Property(r => r.RowVersion).IsRowVersion();
        });
    }

    private void RegisterSymbolEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SymbolsServerRequest>(entity =>
        {
            entity.HasKey(r => r.Key);
            entity.HasIndex(r => r.SymbolsKey);
            entity.Property(s => s.RowVersion).IsRowVersion();
        });
    }

    private void RegisterCvsScanEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentScanOperationState>(entity =>
        {
            entity.HasKey(p => p.Key);

            entity.HasIndex(s => new { s.ValidationStepId, s.Type, s.Status });
            entity.HasIndex(s => s.CreatedAt);

            entity.Property(s => s.CreatedAt).IsRequired().HasColumnType("datetime2");
            entity.Property(s => s.FinishedAt).HasColumnType("datetime2");
            entity.Property(s => s.PolledAt).HasColumnType("datetime2");
            entity.Property(s => s.ContentPath).HasMaxLength(4096);
            entity.Property(s => s.FileId).HasMaxLength(64);
            entity.Property(pvs => pvs.RowVersion).IsRowVersion();
        });
    }

    protected virtual void ConfigureBatchIdProperty(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValidatorStatus>()
            .Ignore(s => s.BatchId);
    }
}
