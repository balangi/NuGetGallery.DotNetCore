// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGet.Services.Entities;
using NuGet.Services.Messaging;
using NuGet.Services.Messaging.Email;
using NuGet.Services.ServiceBus;
using NuGetGallery.Auditing;
using NuGetGallery.Configuration;
using NuGetGallery.Cookies;
using NuGetGallery.Features;
using NuGetGallery.Infrastructure;
using NuGetGallery.Infrastructure.Search;
using NuGetGallery.Services;

namespace NuGetGallery.Infrastructures.AutofacModules
{
    /// <summary>
    /// Default registrations for the Gallery application. Configuration is supplied by the
    /// ASP.NET Core options system; application services remain Autofac-managed so keyed
    /// registrations and the Gallery's existing module boundaries are preserved.
    /// </summary>
    public sealed class DefaultDependenciesModule : Module
    {
        public static class BindingKeys
        {
            public const string AsyncDeleteAccountName = "AsyncDeleteAccountService";
            public const string SyncDeleteAccountName = "SyncDeleteAccountService";
            public const string PrimaryStatisticsKey = "PrimaryStatisticsKey";
            public const string AlternateStatisticsKey = "AlternateStatisticsKey";
            public const string FeatureFlaggedStatisticsKey = "FeatureFlaggedStatisticsKey";
            public const string AccountDeleterTopic = "AccountDeleterBindingKey";
            public const string StagingPromotionTopic = "StagingPromotionBindingKey";
            public const string PackageValidationTopic = "PackageValidationBindingKey";
            public const string SymbolsPackageValidationTopic = "SymbolsPackageValidationBindingKey";
            public const string PackageValidationEnqueuer = "PackageValidationEnqueuerBindingKey";
            public const string SymbolsPackageValidationEnqueuer = "SymbolsPackageValidationEnqueuerBindingKey";
            public const string EmailPublisherTopic = "EmailPublisherBindingKey";
            public const string PreviewSearchClient = "PreviewSearchClientBindingKey";
            public const string AuditKey = "AuditKey";
        }

        public static class ParameterNames
        {
            public const string PackagesController_PreviewSearchService = "previewSearchService";
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            // The host owns configuration binding. The same options instance is used to expose
            // the two Gallery-facing configuration abstractions expected by existing services.
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value)
                .AsSelf().As<IAppConfiguration>().As<IGalleryConfigurationService>()
                .SingleInstance();

            builder.RegisterType<CredentialBuilder>().As<ICredentialBuilder>().SingleInstance();
            builder.RegisterType<CredentialValidator>().As<ICredentialValidator>().SingleInstance();
            builder.RegisterType<DateTimeProvider>().As<IDateTimeProvider>().SingleInstance();
            builder.RegisterType<HttpContextCacheService>().As<ICacheService>().InstancePerLifetimeScope();

            ConfigureFederatedCredentials(builder);
            RegisterApplicationInsights(builder);
            RegisterABTestServices(builder);
            RegisterFeatureFlagsService(builder);
            RegisterStatisticsServices(builder);
            RegisterDeleteAccountService(builder);
            RegisterStagingPromotionMessaging(builder);
            RegisterAsynchronousValidation(builder);
            RegisterMessagingService(builder);
            ConfigureSearch(builder);
            ConfigureAutocomplete(builder);
            ConfigureStorage(builder);
            RegisterAuditingServices(builder);
            RegisterCookieComplianceService(builder);
            RegisterTyposquattingServiceHelper(builder);

            // Common Gallery services, with explicit scopes (no Autofac default lifetime).
            builder.RegisterType<EntityRepository<User>>().As<IEntityRepository<User>>().InstancePerLifetimeScope();
            builder.RegisterType<EntityRepository<Role>>().As<IEntityRepository<Role>>().InstancePerLifetimeScope();
            builder.RegisterType<PackageService>().As<IPackageService>().InstancePerLifetimeScope();
            builder.RegisterType<UserService>().As<IUserService>().InstancePerLifetimeScope();
            builder.RegisterType<PackageOwnershipManagementService>().As<IPackageOwnershipManagementService>().InstancePerLifetimeScope();
        }

        private static void ConfigureFederatedCredentials(ContainerBuilder builder)
        {
            builder.RegisterType<FederatedCredentialRepository>().As<IFederatedCredentialRepository>().InstancePerLifetimeScope();
            builder.RegisterType<FederatedCredentialService>().As<IFederatedCredentialService>().InstancePerLifetimeScope();
            builder.RegisterType<FederatedCredentialPolicyEvaluator>().As<IFederatedCredentialPolicyEvaluator>().InstancePerLifetimeScope();
        }

        private static void RegisterApplicationInsights(ContainerBuilder builder)
        {
            // The host registers the modern Application Insights TelemetryClient/configuration.
            // Register Gallery's telemetry facade through its concrete service when available.
            builder.RegisterType<TelemetryService>().As<ITelemetryService>().As<IFeatureFlagTelemetryService>()
                .SingleInstance();
        }

        private static void RegisterABTestServices(ContainerBuilder builder)
        {
            builder.RegisterType<ABTestService>().As<IABTestService>().InstancePerLifetimeScope();
        }

        private static void RegisterDeleteAccountService(ContainerBuilder builder)
        {
            builder.RegisterType<DeleteAccountService>().Keyed<IDeleteAccountService>(BindingKeys.SyncDeleteAccountName)
                .InstancePerLifetimeScope();
            builder.RegisterType<AsynchronousDeleteAccountService>().Keyed<IDeleteAccountService>(BindingKeys.AsyncDeleteAccountName)
                .InstancePerLifetimeScope();
            builder.Register(c => c.ResolveKeyed<IDeleteAccountService>(
                    c.Resolve<IOptions<WebApplicationConfiguration>>().Value.AsynchronousDeleteAccountServiceEnabled
                        ? BindingKeys.AsyncDeleteAccountName : BindingKeys.SyncDeleteAccountName))
                .As<IDeleteAccountService>().InstancePerLifetimeScope();
        }

        private static void RegisterStatisticsServices(ContainerBuilder builder)
        {
            builder.RegisterType<JsonStatisticsService>().As<IStatisticsService>().SingleInstance();
            builder.RegisterType<CloudDownloadCountService>().As<IDownloadCountService>().SingleInstance();
            builder.RegisterType<CloudReportService>().As<IReportService>().SingleInstance();
            builder.RegisterType<JsonAggregateStatsService>().As<IAggregateStatsService>().SingleInstance();
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value.AzureStorage_Statistics_ConnectionString)
                .Keyed<string>(BindingKeys.PrimaryStatisticsKey).SingleInstance();
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value.AzureStorage_Statistics_ConnectionString_Alternate)
                .Keyed<string>(BindingKeys.AlternateStatisticsKey).SingleInstance();
        }

        private static void RegisterSwitchingDeleteAccountService(ContainerBuilder builder)
        {
            RegisterDeleteAccountService(builder);
        }

        private static void RegisterStagingPromotionMessaging(ContainerBuilder builder)
        {
            builder.RegisterType<StagingPromotionMessageSerializer>().As<IBrokeredMessageSerializer<StagingPromotionMessage>>().SingleInstance();
            builder.RegisterType<StagingPromotionMessageEnqueuer>().As<IStagingPromotionMessageEnqueuer>().InstancePerLifetimeScope();
        }

        private static void RegisterFeatureFlagsService(ContainerBuilder builder)
        {
            builder.RegisterType<FeatureFlagCacheService>().As<IFeatureFlagCacheService>().SingleInstance();
            builder.RegisterType<FeatureFlagClient>().As<IFeatureFlagClient>().SingleInstance();
            builder.RegisterType<FeatureFlagService>().As<IFeatureFlagService>().SingleInstance();
        }

        private static void RegisterMessagingService(ContainerBuilder builder)
        {
            // A host can replace IMessageService with the SMTP or broker-backed registration.
            // Choose using the same switch as the Gallery's configuration model.
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value.AsynchronousEmailServiceEnabled)
                .As<bool>().SingleInstance();
            builder.RegisterType<AsynchronousEmailMessageService>().As<IMessageService>().InstancePerDependency();
        }

        private static void RegisterSmtpEmailMessagingService(ContainerBuilder builder)
        {
            builder.RegisterType<BackgroundMarkdownMessageService>().As<IMessageService>().InstancePerDependency();
        }

        private static void RegisterAsynchronousEmailMessagingService(ContainerBuilder builder)
        {
            builder.RegisterType<NuGet.Services.Messaging.ServiceBusMessageSerializer>()
                .As<NuGet.Services.Messaging.IServiceBusMessageSerializer>().SingleInstance();
            builder.RegisterType<EmailMessageEnqueuer>().As<IEmailMessageEnqueuer>().InstancePerLifetimeScope();
            builder.RegisterType<AsynchronousEmailMessageService>().As<IMessageService>().InstancePerDependency();
        }

        private static void ConfigureSearch(ContainerBuilder builder)
        {
            RegisterSearchService(builder);
        }

        private static void RegisterSearchService(ContainerBuilder builder)
        {
            builder.RegisterType<LuceneSearchService>().As<ISearchService>().InstancePerLifetimeScope();
            builder.RegisterType<LuceneSearchService>().Keyed<ISearchService>(BindingKeys.PreviewSearchClient)
                .InstancePerLifetimeScope();
            builder.RegisterType<LuceneIndexingService>().As<IIndexingService>().InstancePerLifetimeScope();
        }

        private static void ConfigureAutocomplete(ContainerBuilder builder)
        {
            builder.RegisterType<AutocompleteDatabasePackageIdsQuery>().As<IAutocompletePackageIdsQuery>().InstancePerLifetimeScope();
            builder.RegisterType<AutocompleteDatabasePackageVersionsQuery>().As<IAutocompletePackageVersionsQuery>().InstancePerLifetimeScope();
        }

        private static void ConfigureStorage(ContainerBuilder builder)
        {
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value.StorageType)
                .As<string>().SingleInstance();
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value.StorageType == StorageType.AzureStorage
                    ? (IFileStorageService)c.Resolve<AzureFileStorageService>()
                    : c.Resolve<FileSystemFileStorageService>())
                .As<IFileStorageService>().As<ICoreFileStorageService>().SingleInstance();
            ConfigureForLocalFileSystem(builder);
            ConfigureForAzureStorage(builder);
        }

        private static void ConfigureForLocalFileSystem(ContainerBuilder builder)
        {
            builder.RegisterType<FileSystemService>().As<IFileSystemService>().SingleInstance();
            builder.RegisterType<FileSystemFileStorageService>().AsSelf().SingleInstance();
        }

        private static void ConfigureForAzureStorage(ContainerBuilder builder)
        {
            builder.RegisterType<AzureFileStorageService>().AsSelf().SingleInstance();
        }

        private static void CombineAuditingServices(ContainerBuilder builder)
        {
            builder.RegisterType<AuditingService>().As<IAuditingService>().InstancePerLifetimeScope();
        }

        private static void RegisterAuditingServices(ContainerBuilder builder)
        {
            CombineAuditingServices(builder);
            builder.Register(c => c.Resolve<IOptions<WebApplicationConfiguration>>().Value.StorageType == StorageType.AzureStorage
                    ? c.Resolve<IAuditingService>() : c.Resolve<IAuditingService>())
                .As<IAuditingService>().InstancePerLifetimeScope();
        }

        private static void RegisterCookieComplianceService(ContainerBuilder builder)
        {
            builder.RegisterType<CookieExpirationService>().As<ICookieExpirationService>().SingleInstance();
            builder.RegisterType<CookieComplianceService>().As<ICookieComplianceService>().SingleInstance();
        }

        private static void RegisterTyposquattingServiceHelper(ContainerBuilder builder)
        {
            builder.RegisterType<ExactMatchTyposquattingServiceHelper>().As<ITyposquattingServiceHelper>().SingleInstance();
            builder.RegisterType<TyposquattingService>().As<ITyposquattingService>().InstancePerLifetimeScope();
        }

        private static ISqlConnectionFactory CreateDbConnectionFactory(string name, string connectionString)
        {
            return new AzureSqlConnectionFactory(connectionString, NullCachingSecretInjector.Instance,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger("AzureSqlConnectionFactory-" + name));
        }

        private static void ConfigureGalleryReadOnlyReplicaEntitiesContext(ContainerBuilder builder)
        {
            builder.Register(c => BuildDbOptions<ReadOnlyEntitiesContext>(
                    c.Resolve<IOptions<WebApplicationConfiguration>>().Value.SqlReadOnlyReplicaConnectionString
                    ?? c.Resolve<IOptions<WebApplicationConfiguration>>().Value.SqlConnectionString))
                .As<DbContextOptions<ReadOnlyEntitiesContext>>().InstancePerLifetimeScope();
            builder.RegisterType<ReadOnlyEntitiesContext>().As<IReadOnlyEntitiesContext>().InstancePerLifetimeScope();
        }

        private static void ConfigureValidationEntitiesContext(ContainerBuilder builder)
        {
            builder.Register(c => BuildDbOptions<ValidationEntitiesContext>(
                    c.Resolve<IOptions<WebApplicationConfiguration>>().Value.SqlConnectionStringValidation))
                .As<DbContextOptions<ValidationEntitiesContext>>().InstancePerLifetimeScope();
            builder.RegisterType<ValidationEntitiesContext>().AsSelf().InstancePerLifetimeScope();
        }

        private static DbContextOptions<TContext> BuildDbOptions<TContext>(string connectionString) where TContext : DbContext
        {
            var options = new DbContextOptionsBuilder<TContext>();
            options.UseSqlServer(connectionString);
            return options.Options;
        }

        private static void RegisterAsynchronousValidation(ContainerBuilder builder)
        {
            builder.RegisterType<PackageValidationMessageSerializer>().As<IBrokeredMessageSerializer<PackageValidationMessage>>().SingleInstance();
            builder.RegisterType<PackageValidationEnqueuer>().Keyed<IPackageValidationEnqueuer>(BindingKeys.PackageValidationEnqueuer)
                .InstancePerLifetimeScope();
            builder.RegisterType<SymbolsPackageValidationEnqueuer>().Keyed<IPackageValidationEnqueuer>(BindingKeys.SymbolsPackageValidationEnqueuer)
                .InstancePerLifetimeScope();
            ConfigureValidationEntitiesContext(builder);
        }
    }
}
