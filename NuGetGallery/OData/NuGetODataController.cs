// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData;
using NuGetGallery.Configuration;
using NuGetGallery.OData.QueryFilter;

namespace NuGetGallery.OData;

public abstract class NuGetODataController
    : ODataController
{
    private readonly IGalleryConfigurationService _configurationService;
    protected readonly ITelemetryService _telemetryService;

    protected const string Id = "Id";
    protected const string Version = "Version";

    private readonly IHttpContextAccessor _httpContextAccessor;

    protected NuGetODataController(
        IGalleryConfigurationService configurationService,
        ITelemetryService telemetryService,
        IHttpContextAccessor httpContextAccessor)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        ODataQueryVerifier = ODataQueryVerifier.Build(telemetryService);
    }

    internal ODataQueryVerifier ODataQueryVerifier { get; }

    protected virtual HttpContext GetTraditionalHttpContext()
    {
        return _httpContextAccessor.HttpContext;
    }

    protected virtual bool UseHttps()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        return request != null && string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase);
    }

    protected virtual string GetSiteRoot()
    {
        return _configurationService.GetSiteRoot(UseHttps()).TrimEnd('/') + '/';
    }

    protected IActionResult DeprecatedRequest(string message)
    {
        var error = new ODataError
        {
            Code = "NuGet.V2.Deprecated",
            Message = message
        };
        return BadRequest(error);
    }

    /// <summary>
    /// Generates a QueryResult.
    /// </summary>
    protected virtual IActionResult TrackedQueryResult<TModel>(
        ODataQueryOptions<TModel> options,
        IQueryable<TModel> queryable,
        int maxPageSize,
        bool? customQuery)
    {
        _telemetryService.TrackODataCustomQuery(customQuery);

        // در ASP.NET Core OData نتایج مستقیماً به شکل Ok یا QueryResult سازگار بازگردانده می‌شوند
        return Ok(queryable);
    }

    /// <summary>
    /// Generates a QueryResult that is already paged.
    /// </summary>
    protected virtual IActionResult TrackedQueryResult<TModel>(
        ODataQueryOptions<TModel> options,
        IQueryable<TModel> queryable,
        int maxPageSize,
        long totalResults,
        Func<ODataQueryOptions<TModel>, ODataQuerySettings, long?, Uri> generateNextLink,
        bool? customQuery)
    {
        _telemetryService.TrackODataCustomQuery(customQuery);

        return Ok(queryable);
    }

    /// <summary>
    /// The OrderById will be skipped when the request has an OrderBy clause that does not include Id 
    /// </summary>
    protected bool TryShouldIgnoreOrderById<T>(ODataQueryOptions<T> options, out bool shouldIgnoreOrderById)
    {
        shouldIgnoreOrderById = false;

        try
        {
            shouldIgnoreOrderById = options.OrderBy != null &&
                   !options.OrderBy.OrderByNodes.Any((node) =>
                   {
                       string nodeName = ((OrderByPropertyNode)node).Property.Name;
                       return string.Equals(nodeName, Id, StringComparison.Ordinal);
                   });
        }
        catch (ODataException)
        {
            return false;
        }

        return true;
    }
}
