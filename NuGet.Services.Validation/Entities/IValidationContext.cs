// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using NuGet.Services.Validation.Entities;

namespace NuGet.Services.Validation;

public interface IValidationEntitiesContext
{
    DbSet<PackageValidationSet> PackageValidationSets { get; }
    DbSet<PackageValidation> PackageValidations { get; }
    DbSet<PackageValidationIssue> PackageValidationIssues { get; }
    DbSet<ValidatorStatus> ValidatorStatuses { get; }
    DbSet<PackageSigningState> PackageSigningStates { get; }
    DbSet<PackageSignature> PackageSignatures { get; }
    DbSet<TrustedTimestamp> TrustedTimestamps { get; }
    DbSet<EndCertificate> EndCertificates { get; }
    DbSet<EndCertificateValidation> CertificateValidations { get; }
    DbSet<CertificateChainLink> CertificateChainLinks { get; }
    DbSet<ParentCertificate> ParentCertificates { get; }
    DbSet<PackageCompatibilityIssue> PackageCompatibilityIssues { get; }
    DbSet<ScanOperationState> ScanOperationStates { get; }
    DbSet<PackageRevalidation> PackageRevalidations { get; set; }
    DbSet<SymbolsServerRequest> SymbolsServerRequests { get; set; }
    DbSet<ContentScanOperationState> ContentScanOperationState { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
