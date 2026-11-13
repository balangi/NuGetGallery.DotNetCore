// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NuGetGallery;

/// <summary>
/// معادل intercept رویداد ObjectContext.ObjectMaterialized در EF6.
/// هر بار که یک شیء (entity) از پایگاه داده materialize می‌شود،
/// این متد صدا زده می‌شود.
/// </summary>
public class ObjectMaterializedInterceptor : IMaterializationInterceptor
{
    private readonly Action<object> _onMaterialized;

    public ObjectMaterializedInterceptor(Action<object> onMaterialized)
    {
        _onMaterialized = onMaterialized ?? throw new ArgumentNullException(nameof(onMaterialized));
    }

    // این متد بعد از مقداردهی اولیه‌ی نمونه (instance) فراخوانی می‌شود.
    public object InitializedInstance(
        MaterializationInterceptionData materializationData,
        object instance)
    {
        // همان منطقی که در EF6 در ObjectContextOnObjectMaterialized بود را این‌جا انجام می‌دهیم
        ObjectMaterializedInterception.InterceptObjectMaterialized(instance);

        // می‌توانید علاوه بر آن اکشن دلخواه را هم اجرا کنید
        _onMaterialized?.Invoke(instance);

        return instance;
    }
}
