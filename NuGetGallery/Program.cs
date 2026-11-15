using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using NuGetGallery;
using NuGetGallery.Configuration;
using NuGetGallery.Diagnostics;
using NuGetGallery.Infrastructure.Constraints;
using NuGetGallery.Services;

var builder = WebApplication.CreateBuilder(args);

// ۱. تنظیم IoC Container با Autofac (سازگار با DefaultDependenciesModule)
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // ثبت ماژول اصلی دیپندنسی‌های گالری
    containerBuilder.RegisterModule(new DefaultDependenciesModule());
});

// ۲. خواندن تنظیمات برنامه‌نویسی (معادل AppConfiguration)
builder.Services.Configure<WebApplicationConfiguration>(builder.Configuration.GetSection("Gallery"));
builder.Services.AddSingleton<IAppConfiguration>(sp => 
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WebApplicationConfiguration>>().Value);

// ۳. اتصال پایگاه داده (Entity Framework Core)
builder.Services.AddDbContext<EntitiesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GalleryDB"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5, // تعداد دفعات تلاش مجدد
            maxRetryDelay: TimeSpan.FromSeconds(30), // حداکثر زمان بین تلاش‌ها
            errorNumbersToAdd: null // لیستی از کدهای خطای SQL که باید retry شوند (null یعنی همه خطاهای قابل retry)
        )));

// ۴. ثبت سرویس‌های MVC و OData (بر اساس کانفیگ‌های NuGetODataConfig)
builder.Services.AddControllersWithViews(options =>
{
    // ثبت فیلترهای گلوبال (مشابه GlobalFilters در AppPostStart)
    options.Filters.Add<SendErrorsToTelemetryAttribute>();
    options.Filters.Add<ReadOnlyModeErrorFilter>();
    options.Filters.Add<AntiForgeryErrorFilter>();
    options.Filters.Add<UserDeletedErrorFilter>();
    options.Filters.Add<RequestValidationExceptionFilter>();
})
.AddNewtonsoftJson()
.AddOData(options =>
{
    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
    // راه‌اندازی EDM مدل‌های V2 مشابه NuGetODataV2FeedConfig
    options.AddRouteComponents("api/v2", ODataEdmModel.GetEdmModelV2());
});

// ۵. احراز هویت و کوکی (جایگزین مکانیزم امنیتی OwinStartup)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "NuGetGallery.Cookies";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/users/account/logon";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AuthenticationTypes.LocalUser;
        options.DefaultSignInScheme = AuthenticationTypes.LocalUser;
        options.DefaultChallengeScheme = AuthenticationTypes.LocalUser;
    });

builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        options =>
        {
            var apiKeyConfig = builder.Configuration
                .GetSection("ApiKey")
                .Get<ApiKeyAuthenticatorConfiguration>();

            if (apiKeyConfig is not null)
            {
                options.ApiKeyHeaderName = apiKeyConfig.HeaderName;
                options.ApiKeyClaim = apiKeyConfig.Claim;
            }
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyOnly", policy =>
        policy.AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme)
              .RequireAuthenticatedUser());
});


// ۶. سرویس‌های پس‌زمینه و تله‌متری (جایگزین WebBackgrounder در AppActivator)
builder.Services.AddHostedService<CloudDownloadCountServiceRefreshHostedService>();
builder.Services.AddHostedService<PackageVulnerabilitiesCacheRefreshHostedService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITelemetryClient, AppInsightsTelemetryClient>();

var app = builder.Build();

// ==========================================
// تنظیمات Pipeline
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/errors/500");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // جایگزین Bundling و فایل‌های استاتیک

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ۷. ثبت روت‌ها و اندپوینت‌ها (معادل Routes.cs و متدهای آن)
app.UseEndpoints(endpoints =>
{
    // روت‌های ادمین (در صورت فعال بودن AdminPanel)
    endpoints.MapControllerRoute(
        name: "AdminArea",
        pattern: "admin/{controller=Home}/{action=Index}/{id?}");

    // روت نمایش پکیج با استفاده از Constraint اختصاصی VersionRouteConstraint
    endpoints.MapControllerRoute(
        name: "DisplayPackage",
        pattern: "packages/{id}/{version?}",
        defaults: new { controller = "Packages", action = "DisplayPackage" },
        constraints: new { version = new VersionRouteConstraint() });

    // روت پیش‌فرض UI (مشابه RegisterUIRoutes)
    endpoints.MapControllerRoute(
        name: "Default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // روت‌های کنترلرها و APIها
    endpoints.MapControllers();
});

app.Run();
