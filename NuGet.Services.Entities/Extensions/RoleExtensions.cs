// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.Entities;

public static class RoleExtensions
{
    public static bool Is(this Role role, string roleName)
    {
        return string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase);
    }
}
