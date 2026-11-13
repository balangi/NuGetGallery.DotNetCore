// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace NuGetGallery;

public class ObjectMaterializedInterceptingDbContext : DbContext
{
    private readonly Action<object> _onMaterialized;

    public ObjectMaterializedInterceptingDbContext(
        DbContextOptions options,
        Action<object> onMaterialized)
        : base(options)
    {
        _onMaterialized = onMaterialized ?? throw new ArgumentNullException(nameof(onMaterialized));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // اینترسپتور را در این‌جا ثبت می‌کنیم
        optionsBuilder.AddInterceptors(new ObjectMaterializedInterceptor(_onMaterialized));

        base.OnConfiguring(optionsBuilder);
    }
}
