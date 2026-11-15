// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Html;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;

namespace NuGetGallery;

public static class IEnumerableExtensions
{
    public static void AddOrSet<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key, TValue val)
    {
        self.AddOrUpdate(key, val, (_, __) => val);
    }

    public static string Flatten(this IEnumerable<string> list)
    {
        if (list == null)
        {
            return String.Empty;
        }

        return String.Join(", ", list);
    }

    public static IHtmlContent Flatten<T>(this IEnumerable<T> items, Func<T, IHtmlContent> template)
    {
        if (items == null)
        {
            return HtmlString.Empty;
        }

        return new HtmlContentBuilder().AppendHtml(
            string.Join(", ", items.Select(item => RenderHtmlContent(template(item))))
        );
    }

    private static string RenderHtmlContent(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    public static bool AnySafe<T>(this IEnumerable<T> items)
    {
        if (items == null)
        {
            return false;
        }
        return items.Any();
    }

    public static bool AnySafe<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
        if (items == null)
        {
            return false;
        }
        return items.Any(predicate);
    }
}
