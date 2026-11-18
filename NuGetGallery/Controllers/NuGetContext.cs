// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Services.Entities;
using NuGetGallery.Configuration;

namespace NuGetGallery;

public class NuGetContext
{
    private readonly Lazy<User> _currentUser;

    public NuGetContext(AppController ctrl, IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
    {
        if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        if (httpContextAccessor == null) throw new ArgumentNullException(nameof(httpContextAccessor));

        // جایگزینی DependencyResolver با IServiceProvider در Core
        Config = serviceProvider.GetRequiredService<IGalleryConfigurationService>();

        // جایگزینی OwinContext با خواندن کاربر از کنترلر یا HttpContext
        _currentUser = new Lazy<User>(() => ctrl.GetCurrentUser() ?? httpContextAccessor.HttpContext?.User?.GetNuGetUser());
    }

    public IGalleryConfigurationService Config { get; internal set; }

    public User CurrentUser => _currentUser.Value;
}
