// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.Extensions.Primitives;
using NuGetGallery.OData.QueryFilter;

namespace NuGetGallery.WebApi;

public class QueryResult<TModel>:ActionResult
{
    private readonly ODataQueryOptions<TModel> _queryOptions;
    private readonly IQueryable<TModel> _queryable;
    private readonly ControllerBase _controller;
    private readonly long? _totalResults;
    private readonly Func<ODataQueryOptions<TModel>, ODataQuerySettings, long?, Uri> _generateNextLink;
    private readonly bool? _customQuery;
    private readonly bool _isPagedResult;
    private readonly int? _semVerLevelKey;

    private readonly ODataQuerySettings _querySettings;

    public bool FormatAsCountResult { get; set; }
    public bool FormatAsSingleResult { get; set; }

    public QueryResult(
        ODataQueryOptions<TModel> queryOptions,
        IQueryable<TModel> queryable,
        ControllerBase controller,
        int maxPageSize,
        bool? customQuery)
        : this(queryOptions, queryable, controller, maxPageSize, null, null, customQuery)
    {
    }

    public QueryResult(
        ODataQueryOptions<TModel> queryOptions,
        IQueryable<TModel> queryable,
        ControllerBase controller,
        int maxPageSize,
        long? totalResults,
        Func<ODataQueryOptions<TModel>, ODataQuerySettings, long?, Uri> generateNextLink,
        bool? customQuery)
    {
        _queryOptions = queryOptions;
        _queryable = queryable;
        _controller = controller;
        _totalResults = totalResults;
        _generateNextLink = generateNextLink;
        _customQuery = customQuery;

        var request = _controller.Request;
        if (request.Query.TryGetValue("semVerLevel", out StringValues semVerVal))
        {
            _semVerLevelKey = SemVerLevelKey.ForSemVerLevel(semVerVal.FirstOrDefault());
        }

        if (_totalResults.HasValue && generateNextLink != null)
        {
            _isPagedResult = true;
        }

        _querySettings = new ODataQuerySettings()
        {
            PageSize = maxPageSize
        };
    }

    public IActionResult Execute()
    {
        IActionResult result;
        try
        {
            result = GetInnerResult();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            QuietLog.LogHandledException(e);
            throw;
        }

        if (_customQuery.HasValue)
        {
            _controller.Response.Headers[GalleryConstants.CustomQueryHeaderName] =
                _customQuery.Value ? "true" : "false";
        }

        return result;
    }

    public IActionResult GetInnerResult()
    {
        IQueryable queryResults = null;

        if (FormatAsCountResult)
        {
            _querySettings.PageSize = null;
        }

        var queryOptions = _queryOptions;

        if (_isPagedResult && _generateNextLink != null && _totalResults.HasValue)
        {
            queryResults = _queryable.ToList().AsQueryable();

            if (queryOptions?.Filter != null)
            {
                if (_semVerLevelKey != SemVerLevelKey.Unknown
                    && (string.Equals(queryOptions.Filter.RawValue, ODataQueryFilter.IsLatestVersion, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(queryOptions.Filter.RawValue, ODataQueryFilter.IsAbsoluteLatestVersion, StringComparison.OrdinalIgnoreCase)))
                {
                    // Skip redundant OData filter
                }
                else
                {
                    queryResults = queryOptions.Filter.ApplyTo(queryResults, _querySettings);
                }
            }

            if (queryOptions?.OrderBy != null)
            {
                queryResults = queryOptions.OrderBy.ApplyTo(queryResults, _querySettings);
            }

            if (queryOptions?.Top != null)
            {
                queryResults = queryOptions.Top.ApplyTo(queryResults, _querySettings);
            }

            queryOptions = null;
        }
        else
        {
            queryResults = ExecuteQuery(_queryable, queryOptions);
        }

        var modelQueryResults = queryResults as IQueryable<TModel>;

        if (queryResults == null)
        {
            return _controller.NotFound();
        }

        if (FormatAsSingleResult)
        {
            if (FormatAsCountResult)
            {
                var count = modelQueryResults != null ? Math.Min(1, modelQueryResults.Count()) : 0;
                return Content(count.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                var model = modelQueryResults.FirstOrDefault();
                if (model == null)
                {
                    return _controller.NotFound();
                }
                return _controller.Ok(model);
            }
        }
        else
        {
            if (_isPagedResult && _generateNextLink != null && _totalResults.HasValue)
            {
                if (FormatAsCountResult)
                {
                    return Content(_totalResults.Value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    if (modelQueryResults != null)
                    {
                        return _controller.Ok(new PageResult<TModel>(
                            modelQueryResults,
                            _generateNextLink(_queryOptions, _querySettings, _totalResults),
                            _totalResults));
                    }
                }
            }
            else
            {
                if (FormatAsCountResult)
                {
                    return Content((modelQueryResults?.Count() ?? 0).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    if (modelQueryResults != null)
                    {
                        return _controller.Ok(modelQueryResults);
                    }
                }
            }
        }

        return _controller.BadRequest("Could not execute OData query.");
    }

    private void ValidateQuery(ODataQueryOptions<TModel> queryOptions)
    {
        foreach (var kvp in _controller.Request.Query)
        {
            if (!ODataQueryOptions.IsSystemQueryOption(kvp.Key) &&
                 kvp.Key.StartsWith("$", StringComparison.Ordinal))
            {
                throw new BadHttpRequestException($"Query parameter {kvp.Key} is not supported.");
            }
        }
    }

    private IQueryable ExecuteQuery(IQueryable<TModel> queryable, ODataQueryOptions<TModel> queryOptions)
    {
        if (queryOptions == null)
        {
            return queryable;
        }

        ValidateQuery(queryOptions);
        return queryOptions.ApplyTo(queryable, _querySettings);
    }

    private ContentResult Content(string content)
    {
        return new ContentResult
        {
            Content = content,
            ContentType = "text/plain; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
