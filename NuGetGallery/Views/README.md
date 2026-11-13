# مهاجرت Razor Views به ASP.NET Core 8/9

این بسته یک مهاجرت متمرکز از سه فایل ارائه‌شده در ASP.NET MVC 5 / .NET Framework 4.6 است. فایل‌های زیر را در پروژه وب Core قرار دهید:

- `Views/_ViewStart.cshtml`
- `Views/_ViewImports.cshtml`
- `Views/NuGetViewBase.cs`
- این راهنما در ریشه بسته (`README.md`)

فایل `Views/web.config` عمداً منتقل نشده است: تنظیمات Razor درون `Views/web.config` وابسته به System.Web/MVC 5 هستند و در ASP.NET Core معادل قابل‌کپی ندارند. در صورت میزبانی روی IIS، فایل `web.config` ریشه برنامه ممکن است همچنان برای تنظیمات استقرار IIS لازم باشد؛ آن را با پیکربندی Views اشتباه نگیرید.

## رفتار حفظ‌شده و تغییرات

- `_ViewStart.cshtml` تزریق‌شده‌ی `IWebHostEnvironment` را به‌کار می‌گیرد. اگر `ViewData["Sections"]` از قبل مقدار داشته باشد آن را دست‌نخورده می‌گذارد؛ در غیر این صورت یک `Lazy<List<string>>` ایجاد می‌کند. وجود `Branding/Views/Shared/Layout.cshtml` نسبت به `ContentRootPath` سنجیده می‌شود و سپس همان شناسه‌های اصلی layout انتخاب می‌شوند: `~/Branding/Views/Shared/Layout.cshtml` یا `~/Views/Shared/gallery/Layout.cshtml`.
- دو کلاس پایه با `RazorPage` و `RazorPage<TModel>` جایگزین `WebViewPage` شده‌اند. `NuGetContext` به‌صورت lazy از کلید ViewData یا از سرویس scoped درخواست resolve می‌شود. `Config`، `CurrentUser`، `ContentObjectService`، `ShowAuthInHeader` و (فقط نوع generic) `LinkOpenSearchXml` حفظ شده‌اند.
- `Views/_ViewImports.cshtml` جایگزین importهای مجاز و tag helperهای ASP.NET Core را تعریف می‌کند.

## تزریق NuGetContext و سرویس‌ها

راه ترجیحی برای حفظ همان نمونه‌ی زمینه‌ای که کنترلر استفاده می‌کند، قرار دادن آن در ViewData با کلید `NuGetViewKeys.NuGetContext` است. در `AppController`، در صورت سازگاری با ساختار کنترلر خود، می‌توان این الگو را به‌کار برد:

```csharp
using Microsoft.AspNetCore.Mvc.Filters;
using NuGetGallery.Views;

public override void OnActionExecuting(ActionExecutingContext context)
{
    base.OnActionExecuting(context);
    ViewData[NuGetViewKeys.NuGetContext] = NuGetContext;
}
```

در نمونه‌ی بالا `NuGetContext` باید همان نمونه‌ای باشد که اکشن و ویو باید ببینند. به‌جای این کلید، می‌توان `NuGetContext` را در DI به‌صورت scoped ثبت کرد؛ در این حالت اطمینان دهید کنترلر و ویو دقیقاً همان ثبت scoped را مصرف کنند. اگر هیچ‌یک فراهم نباشد، کلاس پایه با پیام روشن `InvalidOperationException` می‌دهد. `IContentObjectService` نیز باید در DI ثبت شود؛ دسترسی آن با `GetRequiredService<IContentObjectService>()` انجام می‌شود و نبود ثبت، خطای DI ایجاد می‌کند.

ثبت معمول MVC در میزبان Core باید شامل این مورد باشد (در کنار ثبت سرویس‌های برنامه):

```csharp
builder.Services.AddControllersWithViews();
```

## نکات ادغام و محدودیت‌ها

1. `_ViewStart.cshtml` برای viewهای کامل اجرا می‌شود، اما partial viewها آن را اجرا نمی‌کنند؛ partialهایی که به `Sections` یا layout وابسته‌اند باید نیاز خود را جداگانه مدیریت کنند.
2. فایل layout انتخاب‌شده باید در کامپایل و انتشار Razor پروژه گنجانده شود (مثلاً در محل/تنظیمات مناسب پروژه و publish). صرف وجود فیزیکی فایل، تضمین نمی‌کند Razor آن را پیدا یا کامپایل کند.
3. tag helperهای پیش‌فرض Core جایگزین helperهای سفارشی/قدیمی نیستند. tag helperها، HTML helperها و هر استفاده از `Microsoft.Web.Helpers` یا APIهای MVC قدیمی باید جداگانه مهاجرت و ثبت شود؛ این بسته عمداً `System.Web.*` و `Microsoft.Web.Helpers` را import نمی‌کند.
4. `Views/web.config` حذف‌شدنی است و نباید به پروژه Core اضافه شود. `ViewContext.Controller`، `DependencyResolver.Current` و `InitializePage` هم در پیاده‌سازی Core استفاده نشده‌اند.
5. این بسته فقط بر مبنای سه قطعه‌کد داده‌شده ساخته شده است؛ پروژه‌ی کامل، تعریف واقعی `NuGetContext`، `User`، `IGalleryConfigurationService` و `IContentObjectService` و وابستگی‌های آن‌ها در دسترس نبوده است. فرض شده این انواع پروژه‌ای قبلاً برای ASP.NET Core مهاجرت شده‌اند؛ بنابراین کامپایل کل برنامه یا سازگاری تمام viewهای دیگر در اینجا قابل تضمین/آزمایش نیست. `NuGetViewBase.cs` برای کامپایل در همان اسمبلی/پروژه‌ای طراحی شده که این انواع و namespaceهای اصلی را فراهم می‌کند.
