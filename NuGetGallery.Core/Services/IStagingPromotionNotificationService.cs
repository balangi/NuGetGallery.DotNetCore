// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.Entities;

namespace NuGetGallery;

/// <summary>
/// Sends an owner-only result for one promoted staging artifact.
/// </summary>
public interface IStagingPromotionNotificationService
{
    Task SendAsync(User owner, StagingPromotionArtifact artifact);
}
