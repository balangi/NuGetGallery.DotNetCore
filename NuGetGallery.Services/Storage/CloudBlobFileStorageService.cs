// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NuGetGallery.Configuration;
using NuGetGallery.Diagnostics;
using System.Diagnostics;

namespace NuGetGallery;

public class CloudBlobFileStorageService : CloudBlobCoreFileStorageService, IFileStorageService
{
    private readonly IAppConfiguration _configuration;
    private readonly ISourceDestinationRedirectPolicy _redirectPolicy;

    public CloudBlobFileStorageService(
        ICloudBlobClient client,
        IAppConfiguration configuration,
        ISourceDestinationRedirectPolicy redirectPolicy,
        IDiagnosticsService diagnosticsService,
        ICloudBlobContainerInformationProvider cloudBlobFolderInformationProvider)
        : base(client, diagnosticsService, cloudBlobFolderInformationProvider)
    {
        _configuration = configuration;
        _redirectPolicy = redirectPolicy;
    }

    public async Task<IActionResult> CreateDownloadFileActionResultAsync(
        Uri requestUrl,
        string folderName,
        string fileName,
        string versionParameter)
    {
        var container = await GetContainerAsync(folderName);
        var blob = container.GetBlobReference(fileName);

        var redirectUri = GetRedirectUri(requestUrl, blob.Uri, versionParameter);

        return new RedirectResult(redirectUri.AbsoluteUri, permanent: false);
    }

    internal async Task<IActionResult> CreateDownloadFileActionResult(
        HttpContext httpContext,
        string folderName,
        string fileName,
        string versionParameter)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var container = await GetContainerAsync(folderName);
        var blob = container.GetBlobReference(fileName);

        var requestUrl = new Uri(
            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}");

        var redirectUri = GetRedirectUri(requestUrl, blob.Uri, versionParameter);

        return new RedirectResult(redirectUri.AbsoluteUri, permanent: false);
    }

    internal Uri GetRedirectUri(Uri requestUrl, Uri blobUri, string versionParameter)
    {
        if (!_redirectPolicy.IsAllowed(requestUrl, blobUri))
        {
            Trace.TraceInformation(
                "Redirect from {0} to {1} was not allowed",
                requestUrl,
                blobUri);

            throw new InvalidOperationException(
                "Unsafe redirects are not allowed");
        }

        string host;
        int port;
        string scheme;

        if (string.IsNullOrEmpty(_configuration.AzureCdnHost))
        {
            host = blobUri.Host;
            port = blobUri.Port;
            scheme = blobUri.Scheme;
        }
        else
        {
            host = _configuration.AzureCdnHost;
            port = requestUrl.Port;
            scheme = requestUrl.Scheme;
        }

        // When a blob query string is passed, that one always wins.
        // This will only happen on private NuGet gallery instances,
        // not on NuGet.org.
        // When no blob query string is passed, we forward the request
        // URI's query string to the CDN. See https://github.com/NuGet/NuGetGallery/issues/3168
        // and related PR's.
        var queryStringUri = !string.IsNullOrEmpty(blobUri.Query)
            ? blobUri
            : requestUrl;

        var queryValues = ParseQueryString(queryStringUri);

        queryValues[CoreConstants.PackageVersionParameterName] = versionParameter;

        var urlBuilder = new UriBuilder(scheme, host, port)
        {
            Path = blobUri.LocalPath
        };

        return new Uri(
            QueryHelpers.AddQueryString(
                urlBuilder.Uri.AbsoluteUri,
                queryValues));
    }

    public async Task<bool> IsAvailableAsync()
    {
        var container = await GetContainerAsync(
            CoreConstants.Folders.PackagesFolderName);

        return await container.ExistsAsync(cloudBlobLocationMode: null);
    }

    private static Dictionary<string, string> ParseQueryString(Uri uri)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(uri.Query))
        {
            return result;
        }

        var parsed = QueryHelpers.ParseQuery(uri.Query);

        foreach (var pair in parsed)
        {
            result[pair.Key] = pair.Value.ToString();
        }

        return result;
    }
}
