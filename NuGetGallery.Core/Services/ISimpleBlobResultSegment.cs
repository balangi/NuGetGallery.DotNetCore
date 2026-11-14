// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGetGallery;

public interface ISimpleBlobResultSegment
{
    IReadOnlyList<ISimpleCloudBlob> Results { get; }
    BlobListContinuationToken ContinuationToken { get; }
}
