using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using NuGetGallery;
using NuGetGallery.Configuration;
using NuGetGallery.Infrastructure.Constraints;
using NuGetGallery.Infrastructures.AutofacModules;

var builder = WebApplication.CreateBuilder(args);

// ۱. تنظیم IoC Container با Autofac (سازگار با DefaultDependenciesModule)
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // ثبت ماژول اصلی دیپندنسی‌های گالری
    containerBuilder.RegisterModule(new DefaultDependenciesModule());
});

// ۲. خواندن تنظیمات برنامه‌نویسی با کلاس استاندارد مهاجرت‌یافته NuGetGalleryConfigurationOptions
builder.Services.Configure<NuGetGalleryConfigurationOptions>(builder.Configuration.GetSection("Gallery"));

// ۳. اتصال پایگاه داده (Entity Framework Core)
builder.Services.AddDbContext<EntitiesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GalleryDB"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )));

// ۴. ثبت سرویس‌های MVC و OData
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson()
    .AddOData(options =>
    {
        options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
    });

// ۵. احراز هویت و کوکی استاندارد ASP.NET Core
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

builder.Services.AddAuthorization();

// ۶. فعال‌سازی دسترسی به HttpContext
builder.Services.AddHttpContextAccessor();

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
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ۷. ثبت روت‌ها و اندپوینت‌ها (معادل Routes.cs)
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "admin/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "DisplayPackage",
    pattern: "packages/{id}/{version?}",
    defaults: new { controller = "Packages", action = "DisplayPackage" },
    constraints: new { version = new VersionRouteConstraint() });

app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
