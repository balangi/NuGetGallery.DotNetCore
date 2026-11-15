// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NuGet.Services.Entities;
using System.Text;

namespace NuGetGallery;

public static class HttpContextExtensions
{
    // در ASP.NET Core برای دسترسی به Context جاری از IHttpContextAccessor استفاده می‌شود
    public static HttpContext GetCurrent(IServiceProvider serviceProvider)
    {
        var accessor = serviceProvider?.GetService<IHttpContextAccessor>();
        return accessor?.HttpContext;
    }

    // این متد را با یک فراخوانی حدسی جایگزین نکنید.ساده‌ترین اصلاح این است که دریافت کاربر جاری را از این
    // extension حذف و آن را در لایه‌ای انجام دهید که سرویس کاربر را از DI دریافت می‌کند.در
    // HttpContextExtensions فعلاً این متد را حذف کنید:
    //public static User GetCurrentUser(this HttpContext httpContext)
    //{
    //    // در صورت پیاده‌سازی سرویس استخراج کاربر از ClaimsPrincipal
    //    return httpContext?.User?.GetCurrentUser();
    //}

    public static void SetConfirmationReturnUrl(this HttpContext httpContext, string returnUrl)
    {
        var confirmationContext = new ConfirmationContext
        {
            ReturnUrl = returnUrl,
        };
        string json = JsonConvert.SerializeObject(confirmationContext);

        // جایگزینی MachineKey با DataProtection بومی دات‌نت‌کر
        var protector = httpContext.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("ConfirmationContext");

        string protectedJson = Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(json)));

        httpContext.Response.Cookies.Append("ConfirmationContext", protectedJson, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps
        });
    }

    public static string GetConfirmationReturnUrl(this HttpContext httpContext)
    {
        if (httpContext.Request.Cookies == null || !httpContext.Request.Cookies.TryGetValue("ConfirmationContext", out var protectedJson))
        {
            return null;
        }

        if (string.IsNullOrEmpty(protectedJson))
        {
            return null;
        }

        try
        {
            var protector = httpContext.RequestServices
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("ConfirmationContext");

            var unprotectedBytes = protector.Unprotect(Convert.FromBase64String(protectedJson));
            string json = Encoding.UTF8.GetString(unprotectedBytes);

            var confirmationContext = JsonConvert.DeserializeObject<ConfirmationContext>(json);
            return confirmationContext?.ReturnUrl;
        }
        catch
        {
            // اگر رمزگشایی به هر دلیلی (مثل تغییر کلیدها) خطا داد
            return null;
        }
    }

    public static string GetClientInformation(this HttpContext httpContext)
    {
        string userAgent = httpContext.GetUserAgent();
        string result = string.Empty;

        if (!string.IsNullOrEmpty(userAgent))
        {
            int commentPartStartIndex = userAgent.IndexOf('(');

            if (commentPartStartIndex != -1)
            {
                result = userAgent.Substring(0, commentPartStartIndex);
            }
            else
            {
                result = userAgent;
            }

            result = result.Trim();
        }

        return result;
    }

    public static string GetUserAgent(this HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(ServicesConstants.UserAgentHeaderName, out var userAgent))
        {
            return userAgent.ToString();
        }
        return string.Empty;
    }
}

public class ConfirmationContext
{
    public string ReturnUrl { get; set; }
}
