namespace NuGetGallery.Configuration;

public class NuGetGalleryConfigurationOptions
{
    // در اینجا فیلدهایی که در اپلیکیشن استفاده می‌شود را تعریف کنید
    // برای مثال، برخی از تنظیمات رایج در NuGet Gallery:

    public string SiteRoot { get; set; }
    public string PackageStorePath { get; set; }
    public string DatabaseConnectionString { get; set; }

    // اگر تنظیمات دیگری دارید (مثل Azure Storage یا API Keys)، اینجا اضافه کنید
    public bool AllowUploads { get; set; }
}
