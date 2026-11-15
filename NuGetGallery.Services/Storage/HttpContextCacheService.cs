// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Memory;

namespace NuGetGallery;

public sealed class HttpContextCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;

    public HttpContextCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache
            ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public object? GetItem(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Cache key cannot be null or empty.",
                nameof(key));
        }

        return _memoryCache.TryGetValue(key, out var value)
            ? value
            : null;
    }

    public void SetItem(string key, object item, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Cache key cannot be null or empty.",
                nameof(key));
        }

        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "Cache timeout must be greater than zero.");
        }

        _memoryCache.Set(
            key,
            item,
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = timeout
            });
    }

    public void RemoveItem(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Cache key cannot be null or empty.",
                nameof(key));
        }

        _memoryCache.Remove(key);
    }
}
