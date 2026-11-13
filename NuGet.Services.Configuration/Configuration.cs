// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

namespace NuGet.Services.Configuration;

public abstract class Configuration
{
    protected Configuration()
    {
        CreatedTime = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedTime { get; }
}
