// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NuGetGallery;
using NuGet.Services.Entities;
using NuGetGallery.Configuration;

namespace NuGetGallery.Views
{
    /// <summary>Shared key used by AppController to pass the request's gallery context to Razor views.</summary>
    public static class NuGetViewKeys
    {
        public const string NuGetContext = "NuGetContext";
    }

    /// <summary>Resolves the same request-scoped context used by the controller.</summary>
    internal static class NuGetViewContextResolver
    {
        internal static global::NuGetGallery.NuGetContext Resolve(
            ViewContext viewContext,
            string viewDataKey)
        {
            if (viewContext == null)
            {
                throw new InvalidOperationException("NuGetViewBase cannot resolve NuGetContext before a Razor ViewContext is available.");
            }

            if (viewContext.ViewData.TryGetValue(viewDataKey, out var value))
            {
                if (value is global::NuGetGallery.NuGetContext viewDataContext)
                {
                    return viewDataContext;
                }

                throw new InvalidOperationException(
                    $"ViewData[\"{viewDataKey}\"] must contain a non-null NuGetGallery.NuGetContext instance.");
            }

            var requestServices = viewContext.HttpContext?.RequestServices;
            var serviceContext = requestServices?.GetService<global::NuGetGallery.NuGetContext>();
            if (serviceContext != null)
            {
                return serviceContext;
            }

            throw new InvalidOperationException(
                "NuGetViewBase could not find NuGetContext. Set ViewData[NuGetViewKeys.NuGetContext] to the controller's context, or register that same scoped NuGetContext in request services.");
        }
    }

    public abstract class NuGetViewBase : RazorPage
    {
        private readonly Lazy<global::NuGetGallery.NuGetContext> _nugetContext;

        protected NuGetViewBase()
        {
            _nugetContext = new Lazy<global::NuGetGallery.NuGetContext>(
                () => NuGetViewContextResolver.Resolve(ViewContext, NuGetViewKeys.NuGetContext));
        }

        public global::NuGetGallery.NuGetContext NuGetContext => _nugetContext.Value;

        public IGalleryConfigurationService Config => NuGetContext.Config;

        public User CurrentUser => NuGetContext.CurrentUser;

        public Lazy<IContentObjectService> ContentObjectService =>
            new Lazy<IContentObjectService>(() => ViewContext.HttpContext.RequestServices
                .GetRequiredService<IContentObjectService>());

        public bool ShowAuthInHeader => true;
    }

    public abstract class NuGetViewBase<TModel> : RazorPage<TModel>
    {
        private readonly Lazy<global::NuGetGallery.NuGetContext> _nugetContext;

        protected NuGetViewBase()
        {
            _nugetContext = new Lazy<global::NuGetGallery.NuGetContext>(
                () => NuGetViewContextResolver.Resolve(ViewContext, NuGetViewKeys.NuGetContext));
        }

        public global::NuGetGallery.NuGetContext NuGetContext => _nugetContext.Value;

        public IGalleryConfigurationService Config => NuGetContext.Config;

        public User CurrentUser => NuGetContext.CurrentUser;

        public Lazy<IContentObjectService> ContentObjectService =>
            new Lazy<IContentObjectService>(() => ViewContext.HttpContext.RequestServices
                .GetRequiredService<IContentObjectService>());

        public bool ShowAuthInHeader => true;

        public bool LinkOpenSearchXml => true;
    }
}
