// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NuGet.Services.Configuration;
using NuGet.Services.KeyVault;
using NuGetGallery.Configuration.SecretReader;
using NuGetGallery.Services.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace NuGetGallery.Configuration;

public class ConfigurationService : IGalleryConfigurationService, IConfigurationFactory
{
    protected const string SettingPrefix = "Gallery.";
    protected const string FeaturePrefix = "Feature.";
    protected const string ServiceBusPrefix = "AzureServiceBus.";
    protected const string PackageDeletePrefix = "PackageDelete.";
    protected const string FederatedCredentialPrefix = "FederatedCredential.";

    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly Lazy<string> _httpSiteRootThunk;
    private readonly Lazy<string> _httpsSiteRootThunk;
    private readonly Lazy<string> _httpsEmailSupportSiteRootThunk;
    private readonly Lazy<IAppConfiguration> _lazyAppConfiguration;
    private readonly Lazy<FeatureConfiguration> _lazyFeatureConfiguration;
    private readonly Lazy<IServiceBusConfiguration> _lazyServiceBusConfiguration;
    private readonly Lazy<IPackageDeleteConfiguration> _lazyPackageDeleteConfiguration;
    private readonly Lazy<FederatedCredentialConfiguration> _lazyFederatedCredentialConfiguration;

    private static readonly HashSet<string> NotInjectedSettingNames = new(StringComparer.OrdinalIgnoreCase)
    {
        SettingPrefix + "SqlServer",
        SettingPrefix + "SqlServerReadOnlyReplica",
        SettingPrefix + "SupportRequestSqlServer",
        SettingPrefix + "ValidationSqlServer"
    };

    public ICachingSecretInjector SecretInjector { get; set; }

    /// <summary>
    /// Initializes the configuration service and associates a secret injector based on the configured KeyVault settings.
    /// </summary>
    public static ConfigurationService Initialize(IConfiguration configuration = null, IHttpContextAccessor httpContextAccessor = null)
    {
        var configService = new ConfigurationService(configuration, httpContextAccessor);
        var secretReaderFactory = new SecretReaderFactory(configService);
        var secretReader = secretReaderFactory.CreateSecretReader();
        var secretInjector = secretReaderFactory.CreateSecretInjector(secretReader);

        configService.SecretInjector = secretInjector as ICachingSecretInjector
            ?? throw new InvalidOperationException("Caching secret reader is required to run the service");

        return configService;
    }

    public ConfigurationService()
        : this(null, null)
    {
    }

    public ConfigurationService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor = null)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        _httpSiteRootThunk = new Lazy<string>(GetHttpSiteRoot);
        _httpsSiteRootThunk = new Lazy<string>(GetHttpsSiteRoot);
        _httpsEmailSupportSiteRootThunk = new Lazy<string>(GetHttpsSupportEmailSiteRoot);

        _lazyAppConfiguration = new Lazy<IAppConfiguration>(() => ResolveSettings().GetAwaiter().GetResult());
        _lazyFeatureConfiguration = new Lazy<FeatureConfiguration>(() => ResolveFeatures().GetAwaiter().GetResult());
        _lazyServiceBusConfiguration = new Lazy<IServiceBusConfiguration>(() => ResolveServiceBus().GetAwaiter().GetResult());
        _lazyPackageDeleteConfiguration = new Lazy<IPackageDeleteConfiguration>(() => ResolvePackageDelete().GetAwaiter().GetResult());
        _lazyFederatedCredentialConfiguration = new Lazy<FederatedCredentialConfiguration>(() => ResolveFederatedCredential().GetAwaiter().GetResult());
    }

    public static IEnumerable<PropertyDescriptor> GetConfigProperties<T>(T instance)
    {
        return TypeDescriptor.GetProperties(instance).Cast<PropertyDescriptor>().Where(p => !p.IsReadOnly);
    }

    public IAppConfiguration Current => _lazyAppConfiguration.Value;

    public FeatureConfiguration Features => _lazyFeatureConfiguration.Value;

    public IServiceBusConfiguration ServiceBus => _lazyServiceBusConfiguration.Value;

    public IPackageDeleteConfiguration PackageDelete => _lazyPackageDeleteConfiguration.Value;

    public FederatedCredentialConfiguration FederatedCredential => _lazyFederatedCredentialConfiguration.Value;

    public string GetSiteRoot(bool useHttps)
    {
        return useHttps ? _httpsSiteRootThunk.Value : _httpSiteRootThunk.Value;
    }

    public string GetSupportEmailSiteRoot()
    {
        return _httpsEmailSupportSiteRootThunk.Value;
    }

    public Task<T> Get<T>() where T : NuGet.Services.Configuration.Configuration, new()
    {
        var classPrefix = string.Empty;
        var configNamePrefixProperty = (ConfigurationKeyPrefixAttribute)typeof(T)
            .GetCustomAttributes(typeof(ConfigurationKeyPrefixAttribute), inherit: true)
            .FirstOrDefault();

        if (configNamePrefixProperty != null)
        {
            classPrefix = configNamePrefixProperty.Prefix;
        }

        return ResolveConfigObject(Activator.CreateInstance<T>(), classPrefix);
    }

    public async Task<T> ResolveConfigObject<T>(T instance, string prefix)
    {
        foreach (var property in GetConfigProperties<T>(instance))
        {
            string baseName = string.IsNullOrEmpty(property.DisplayName) ? property.Name : property.DisplayName;
            string settingName = prefix + baseName;

            string value = await ReadSettingAsync(settingName);

            if (string.IsNullOrEmpty(value))
            {
                var defaultValue = property.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
                if (defaultValue != null && defaultValue.Value != null)
                {
                    if (defaultValue.Value.GetType() == property.PropertyType)
                    {
                        property.SetValue(instance, defaultValue.Value);
                        continue;
                    }
                    else
                    {
                        value = defaultValue.Value as string;
                    }
                }
            }

            if (!string.IsNullOrEmpty(value))
            {
                if (property.PropertyType.IsAssignableFrom(typeof(string)))
                {
                    property.SetValue(instance, value);
                }
                else if (property.Converter != null && property.Converter.CanConvertFrom(typeof(string)))
                {
                    property.SetValue(instance, property.Converter.ConvertFromString(value));
                }
            }
            else if (property.Attributes.OfType<RequiredAttribute>().Any())
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Missing required configuration setting: '{0}'", settingName));
            }
        }

        return instance;
    }

    public async Task<string> ReadSettingAsync(string settingName)
    {
        var value = ReadRawSetting(settingName);

        if (!string.IsNullOrEmpty(value) && !NotInjectedSettingNames.Contains(settingName) && SecretInjector != null)
        {
            value = await SecretInjector.InjectAsync(value);
        }

        return value;
    }

    public string ReadRawSetting(string settingName)
    {
        var cstr = GetConnectionString(settingName);
        return cstr ?? GetAppSetting(settingName);
    }

    protected virtual HttpRequest GetCurrentRequest()
    {
        return _httpContextAccessor?.HttpContext?.Request;
    }

    private async Task<FeatureConfiguration> ResolveFeatures()
    {
        return await ResolveConfigObject(new FeatureConfiguration(), FeaturePrefix);
    }

    private async Task<IAppConfiguration> ResolveSettings()
    {
        return await ResolveConfigObject(new AppConfiguration(), SettingPrefix);
    }

    private async Task<IServiceBusConfiguration> ResolveServiceBus()
    {
        return await ResolveConfigObject(new ServiceBusConfiguration(), ServiceBusPrefix);
    }

    private async Task<IPackageDeleteConfiguration> ResolvePackageDelete()
    {
        return await ResolveConfigObject(new PackageDeleteConfiguration(), PackageDeletePrefix);
    }

    private async Task<FederatedCredentialConfiguration> ResolveFederatedCredential()
    {
        return await ResolveConfigObject(new FederatedCredentialConfiguration(), FederatedCredentialPrefix);
    }

    protected virtual string GetAppSetting(string settingName)
    {
        if (_configuration == null)
        {
            return Environment.GetEnvironmentVariable(settingName);
        }

        // بررسی به فرمت مسطح (Gallery.SqlServer) یا سلسله‌مراتبی (Gallery:SqlServer)
        return _configuration[settingName] ?? _configuration[settingName.Replace('.', ':')];
    }

    protected virtual string GetConnectionString(string settingName)
    {
        if (_configuration == null)
        {
            return null;
        }

        // اگر نام کلید مستقیم در ConnectionStrings باشد
        var connectionString = _configuration.GetConnectionString(settingName);
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // اگر پیشوند با تنظیمات همراه باشد
        if (settingName.StartsWith(SettingPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var trimmedName = settingName.Substring(SettingPrefix.Length);
            connectionString = _configuration.GetConnectionString(trimmedName);
            if (!string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }
        }

        return null;
    }

    private string GetHttpSiteRoot()
    {
        var siteRoot = Current.SiteRoot;

        CheckValidSiteRoot(ref siteRoot);

        if (siteRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            siteRoot = "http://" + siteRoot.Substring(8);
        }

        return siteRoot;
    }

    private string GetHttpsSiteRoot()
    {
        var siteRoot = _httpSiteRootThunk.Value;

        if (!siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The configured HTTP site root must start with http://.");
        }

        return "https://" + siteRoot.Substring(7);
    }

    private string GetHttpsSupportEmailSiteRoot()
    {
        var siteRoot = Current.SupportEmailSiteRoot;

        CheckValidSiteRoot(ref siteRoot);

        if (siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            siteRoot = "https://" + siteRoot.Substring(7);
        }

        return siteRoot;
    }

    private void CheckValidSiteRoot(ref string siteRoot)
    {
        if (string.IsNullOrEmpty(siteRoot))
        {
            // fallback در صورت نبود SiteRoot مشخص در تنظیمات
            var request = GetCurrentRequest();
            if (request != null)
            {
                siteRoot = $"{request.Scheme}://{request.Host.Value}/";
            }
        }

        if (string.IsNullOrEmpty(siteRoot) ||
            (!siteRoot.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
             !siteRoot.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The configured site root must start with either http:// or https://.");
        }
    }
}
