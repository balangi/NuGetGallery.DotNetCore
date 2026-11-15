// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Html;
using NuGetGallery.Diagnostics;

namespace NuGetGallery;

public class ContentService : IContentService
{
    // Why not use the ASP.Net Cache?
    // Each entry should _always_ have a value. Values never expire, they just need to be updated. Updates use the existing data,
    // so we don't want data just vanishing from a cache.
    private readonly ConcurrentDictionary<string, ContentItem> _contentCache = new(StringComparer.OrdinalIgnoreCase);

    private IDiagnosticsSource Trace { get; set; }

    public IFileStorageService FileStorage { get; protected set; }

    protected ConcurrentDictionary<string, ContentItem> ContentCache => _contentCache;

    protected ContentService()
    {
        Trace = NullDiagnosticsSource.Instance;
    }

    public ContentService(IFileStorageService fileStorage, IDiagnosticsService diagnosticsService)
    {
        FileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));

        if (diagnosticsService == null)
        {
            throw new ArgumentNullException(nameof(diagnosticsService));
        }

        Trace = diagnosticsService.GetSource("ContentService");
    }

    public void ClearCache()
    {
        _contentCache.Clear();
    }

    public Task<IHtmlContent> GetContentItemAsync(string name, TimeSpan expiresIn)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ServicesStrings.ParameterCannotBeNullOrEmpty, nameof(name)), nameof(name));
        }

        return GetContentItemCore(
            name, new[] {
                ServicesConstants.HtmlFileExtension,
                ServicesConstants.MarkdownFileExtension,
                ServicesConstants.JsonFileExtension },
            expiresIn);
    }

    private async Task<IHtmlContent> GetContentItemCore(string name, string[] extensions, TimeSpan expiresIn)
    {
        using (Trace.Activity("GetContentItem " + name))
        {
            ContentItem cachedItem = null;
            if (ContentCache.TryGetValue(name, out cachedItem) && DateTime.UtcNow < cachedItem.ExpiryUtc)
            {
                Trace.Verbose("Cache Valid. Expires at: " + cachedItem.ExpiryUtc);
                return cachedItem.Content;
            }
            Trace.Verbose("Cache Expired.");

            // Get the file from the content service
            var filenames = extensions.Select(extension => name + extension);

            foreach (var filename in filenames)
            {
                ContentItem item = await RefreshContentFromFile(filename, cachedItem, expiresIn);
                if (item != null)
                {
                    // Cache and return the result
                    Debug.Assert(item.Content != null);
                    ContentCache.AddOrSet(name, item);
                    return item.Content;
                }
            }

            Trace.Error($"Requested Content File Not Found: {name}. Extensions tried: {string.Join(", ", extensions)}");
            return new HtmlString(string.Empty);
        }
    }

    private async Task<ContentItem> RefreshContentFromFile(string fileName, ContentItem cachedItem, TimeSpan expiresIn)
    {
        using (Trace.Activity("Downloading Content Item: " + fileName))
        {
            IFileReference reference = await FileStorage.GetFileReferenceAsync(
                CoreConstants.Folders.ContentFolderName,
                fileName,
                ifNoneMatch: cachedItem?.ContentId);

            if (reference == null)
            {
                Trace.Verbose("Requested Content File Not Found: " + fileName);
                return null;
            }

            // Check the content ID to see if it's different
            if (cachedItem != null && string.Equals(cachedItem.ContentId, reference.ContentId, StringComparison.Ordinal))
            {
                Trace.Verbose("No change to content item. Using Cache");

                // Update the expiry time
                cachedItem.ExpiryUtc = DateTime.UtcNow + expiresIn;
                Trace.Verbose($"Updating Cache: {fileName} expires at {cachedItem.ExpiryUtc}");
                return cachedItem;
            }

            // Retrieve the content
            Trace.Verbose("Content Item changed. Trying to update...");
            try
            {
                using (var stream = reference.OpenRead())
                {
                    if (stream == null)
                    {
                        Trace.Error("Requested Content File Not Found: " + fileName);
                        return null;
                    }
                    else
                    {
                        using (Trace.Activity("Reading Content File: " + fileName))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                string text = await reader.ReadToEndAsync();
                                string content;

                                if (fileName.EndsWith(".md", StringComparison.Ordinal))
                                {
                                    content = Markdown.ToHtml(text);
                                }
                                else
                                {
                                    content = text;
                                }

                                IHtmlContent html = new HtmlString(content.Trim());

                                // Prep the new item for the cache
                                var expiryTime = DateTime.UtcNow + expiresIn;
                                return new ContentItem(html, expiryTime, reference.ContentId, DateTime.UtcNow);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Debug.Assert(false, "owchy oochy - reading content failed");
                Trace.Error("Reading updated content failed. Returning cached content instead.");
                return cachedItem;
            }
        }
    }

    public class ContentItem
    {
        public IHtmlContent Content { get; }
        public string ContentId { get; }
        public DateTime RetrievedUtc { get; }
        public DateTime ExpiryUtc { get; set; }

        public ContentItem(IHtmlContent content, DateTime expiryUtc, string contentId, DateTime retrievedUtc)
        {
            Content = content;
            ExpiryUtc = expiryUtc;
            ContentId = contentId;
            RetrievedUtc = retrievedUtc;
        }
    }
}
