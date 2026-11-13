// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGetGallery;

public class CloudBlobGenericNotFoundException : CloudBlobStorageException
{
    public CloudBlobGenericNotFoundException(Exception innerException)
        : base(innerException)
    {
    }

    public CloudBlobGenericNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
