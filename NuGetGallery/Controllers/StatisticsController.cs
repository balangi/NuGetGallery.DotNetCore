// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using NuGet.Versioning;
using NuGetGallery.Configuration;
using NuGetGallery.Services; // فضای نام سرویس‌ها و اکستنشن‌های پروژه

namespace NuGetGallery;

public partial class StatisticsController
    : AppController
{
    private readonly IStatisticsService _statisticsService = null;
    private readonly IAggregateStatsService _aggregateStatsService = null;
    private readonly IAppConfiguration _appConfiguration = null;
    private readonly IUrlHelper _urlHelper = null;

    private static readonly string[] PackageDownloadsByVersionDimensions = new[]
    {
        GalleryConstants.StatisticsDimensions.Version,
        GalleryConstants.StatisticsDimensions.ClientName,
        GalleryConstants.StatisticsDimensions.ClientVersion,
    };

    private static readonly string[] PackageDownloadsDetailDimensions = new[]
    {
        GalleryConstants.StatisticsDimensions.ClientName,
        GalleryConstants.StatisticsDimensions.ClientVersion,
    };

    public StatisticsController(IAggregateStatsService aggregateStatsService)
    {
        _statisticsService = null;
        _aggregateStatsService = aggregateStatsService;
    }

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _aggregateStatsService = null;
    }

    public StatisticsController(
        IStatisticsService statisticsService,
        IAggregateStatsService aggregateStatsService,
        IAppConfiguration appConfiguration,
        IUrlHelper urlHelper)
    {
        _statisticsService = statisticsService;
        _aggregateStatsService = aggregateStatsService;
        _appConfiguration = appConfiguration;
        _urlHelper = urlHelper;
    }

    [HttpGet]
    [HttpHead]
    [OutputCache(Duration = 3600, VaryByHeaderNames = new[] { "Accept-Language" })]
    public virtual async Task<IActionResult> Totals()
    {
        var stats = await _aggregateStatsService.GetAggregateStats();

        return Json(
            new
            {
                Downloads = stats.Downloads,
                UniquePackages = stats.UniquePackages,
                TotalPackages = stats.TotalPackages,
                LastUpdatedDateUtc = stats.LastUpdateDateUtc
            });
    }

    //
    // GET: /stats
    [HttpGet]
    public virtual async Task<IActionResult> Index()
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        await _statisticsService.Refresh();

        var model = new StatisticsPackagesViewModel
        {
            IsDownloadPackageAvailable = _statisticsService.CommunityPackageDownloadsResult.IsLoaded,
            DownloadPackagesSummary = _statisticsService.CommunityPackageDownloadsSummary,

            IsDownloadPackageVersionsAvailable = _statisticsService.CommunityPackageVersionDownloadsResult.IsLoaded,
            DownloadPackageVersionsSummary = _statisticsService.CommunityPackageVersionDownloadsSummary,

            IsNuGetClientVersionAvailable = _statisticsService.NuGetClientVersionResult.IsLoaded,
            NuGetClientVersion = _statisticsService.NuGetClientVersion,

            IsLast6WeeksAvailable = _statisticsService.Last6WeeksResult.IsLoaded,
            Last6Weeks = _statisticsService.Last6Weeks,

            LastUpdatedUtc = _statisticsService.LastUpdatedUtc,
        };

        model.Update();

        model.UseD3 = UseD3();

        return View(model);
    }

    //
    // GET: /stats/packages
    [HttpGet]
    public virtual async Task<IActionResult> Packages()
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        await _statisticsService.Refresh();

        var allPackagesUpdateTime = _statisticsService.PackageDownloadsResult.LastUpdatedUtc;
        var communityPackagesUpdateTime = _statisticsService.CommunityPackageDownloadsResult.LastUpdatedUtc;

        var model = new StatisticsPackagesViewModel
        {
            IsDownloadPackageAvailable = _statisticsService.PackageDownloadsResult.IsLoaded,
            DownloadPackagesAll = _statisticsService.PackageDownloads,

            IsDownloadCommunityPackageAvailable = _statisticsService.CommunityPackageDownloadsResult.IsLoaded,
            DownloadCommunityPackagesAll = _statisticsService.CommunityPackageDownloads,

            LastUpdatedUtc = (allPackagesUpdateTime > communityPackagesUpdateTime)
                                ? allPackagesUpdateTime
                                : communityPackagesUpdateTime,
        };

        return View(model);
    }

    //
    // GET: /stats/packageversions
    [HttpGet]
    public virtual async Task<IActionResult> PackageVersions()
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        await _statisticsService.Refresh();

        var allPackagesUpdateTime = _statisticsService.PackageVersionDownloadsResult.LastUpdatedUtc;
        var communityPackagesUpdateTime = _statisticsService.CommunityPackageVersionDownloadsResult.LastUpdatedUtc;

        var model = new StatisticsPackagesViewModel
        {
            IsDownloadPackageVersionsAvailable = _statisticsService.PackageVersionDownloadsResult.IsLoaded,
            DownloadPackageVersionsAll = _statisticsService.PackageVersionDownloads,

            IsDownloadCommunityPackageVersionsAvailable = _statisticsService.CommunityPackageVersionDownloadsResult.IsLoaded,
            DownloadCommunityPackageVersionsAll = _statisticsService.CommunityPackageVersionDownloads,

            LastUpdatedUtc = (allPackagesUpdateTime > communityPackagesUpdateTime)
                                ? allPackagesUpdateTime
                                : communityPackagesUpdateTime,
        };

        return View(model);
    }

    //
    // GET: /stats/packages/{id}
    [HttpGet]
    public virtual async Task<IActionResult> PackageDownloadsByVersion(string id, string[] groupby)
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        var report = await GetPackageDownloadsByVersionReport(id, groupby);

        var model = new StatisticsPackagesViewModel();

        model.SetPackageDownloadsByVersion(id);

        model.UseD3 = UseD3();

        return View(model);
    }

    //
    // GET: /stats/reports/packages/{id}
    [HttpGet]
    public virtual async Task<IActionResult> PackageDownloadsByVersionReport(string id, string[] groupby)
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        var packageStatisticsReport = await GetPackageDownloadsByVersionReport(id, groupby);

        if (packageStatisticsReport == null)
        {
            return NotFound();
        }

        return Json(packageStatisticsReport);
    }

    //
    // GET: /stats/packages/{id}/{version}
    [HttpGet]
    public virtual async Task<IActionResult> PackageDownloadsDetail(string id, string version, string[] groupby)
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        var report = await GetPackageDownloadsDetailReport(id, version, groupby);

        var model = new StatisticsPackagesViewModel();

        model.SetPackageVersionDownloadsByClient(id, version);

        model.UseD3 = UseD3();

        return View(model);
    }

    //
    // GET: /stats/reports/packages/{id}/{version}
    [HttpGet]
    public virtual async Task<IActionResult> PackageDownloadsDetailReport(string id, string version, string[] groupby)
    {
        if (_statisticsService == NullStatisticsService.Instance)
        {
            return NotFound();
        }

        var report = await GetPackageDownloadsDetailReport(id, version, groupby);

        if (report != null)
        {
            report.Id = MakeReportId(groupby);
        }

        return Json(report);
    }

    private async Task<StatisticsPackagesReport> GetPackageDownloadsByVersionReport(string id, string[] groupby)
    {
        StatisticsPackagesReport report = null;
        try
        {
            report = await _statisticsService.GetPackageDownloadsByVersion(id);

            ProcessReport(report, groupby, PackageDownloadsByVersionDimensions, id);
        }
        catch (StatisticsReportNotFoundException)
        {
            // no report found
        }

        if (report != null)
        {
            report.Id = MakeReportId(groupby);
        }

        return report;
    }

    private async Task<StatisticsPackagesReport> GetPackageDownloadsDetailReport(string id, string version, string[] groupby)
    {
        StatisticsPackagesReport report = null;
        try
        {
            report = await _statisticsService.GetPackageVersionDownloadsByClient(id, version);

            ProcessReport(report, groupby, PackageDownloadsDetailDimensions, null);
        }
        catch (StatisticsReportNotFoundException)
        {
            // no report found
        }

        if (report != null)
        {
            report.Id = MakeReportId(groupby);
        }

        return report;
    }

    private void ProcessReport(StatisticsPackagesReport report, string[] groupby, string[] dimensions, string id)
    {
        if (report == null)
        {
            return;
        }

        var pivot = new string[4];
        if (groupby != null)
        {
            var dim = 0;

            foreach (var dimension in dimensions)
            {
                CheckGroupBy(groupby, dimension, pivot, ref dim, report);
            }

            if (dim == 0)
            {
                groupby = null;
            }
            else
            {
                Array.Resize(ref pivot, dim);
            }
        }

        if (groupby != null)
        {
            var result = StatisticsPivot.GroupBy(report.Facts, pivot);

            if (id != null)
            {
                var col = Array.FindIndex(pivot, s => s.Equals("Version", StringComparison.Ordinal));
                if (col >= 0)
                {
                    for (var row = 0; row < result.Item1.GetLength(0); row++)
                    {
                        var entry = result.Item1[row][col];
                        if (entry != null)
                        {
                            // ارسال آرگومان‌های کامل مورد نیاز امضای متد Package (شامل id, version و relativeUrl)
                            entry.Uri = _urlHelper.Package(id, entry.Data, relativeUrl: false, supportEmail: false);
                        }
                    }
                }
            }

            NuGetVersion prevVersion = null;
            report.Table = result.Item1
                .Select(e =>
                {
                    if (NuGetVersion.TryParse(e[0]?.Data, out NuGetVersion versionOut))
                    {
                        prevVersion = versionOut;
                        return new { version = versionOut, e };
                    }

                    return new { version = prevVersion, e };
                })
                .OrderByDescending(e => e.version)
            .Select(e => e.e).ToList();
            report.Total = result.Item2;
            report.Columns = pivot.Select(GetDimensionDisplayName);
        }

        if (groupby == null)
        {
            foreach (string dimension in dimensions)
            {
                if (!report.Dimensions.Any(d => d.Value == dimension))
                {
                    report.Dimensions.Add(new StatisticsDimension
                    {
                        Value = dimension,
                        DisplayName = GetDimensionDisplayName(dimension),
                        IsChecked = false
                    });
                }
            }

            report.Table = null;
            report.Total = report.Facts.Sum(fact => fact.Amount);
        }
    }

    private static void CheckGroupBy(string[] groupby, string name, string[] pivot, ref int dimension, StatisticsPackagesReport report)
    {
        if (Array.Exists(groupby, s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            pivot[dimension++] = name;
            report.Dimensions.Add(new StatisticsDimension { Value = name, DisplayName = GetDimensionDisplayName(name), IsChecked = true });
        }
        else
        {
            report.Dimensions.Add(new StatisticsDimension { Value = name, DisplayName = GetDimensionDisplayName(name), IsChecked = false });
        }
    }

    private static string GetDimensionDisplayName(string name)
    {
        switch (name)
        {
            case "ClientName":
                return "Client Name";
            case "ClientVersion":
                return "Client Version";
            default:
                return name;
        }
    }

    private static string MakeReportId(string[] groupby)
    {
        string graphId = "report-";
        if (groupby != null)
        {
            foreach (string g in groupby)
            {
                graphId += g;
            }
        }
        return graphId;
    }

    private bool UseD3()
    {
        bool f = true;
        if (Request != null && Request.Headers.TryGetValue("User-Agent", out var userAgentValues))
        {
            string userAgent = userAgentValues.ToString();
            if (userAgent.Contains("MSIE") || userAgent.Contains("Trident/"))
            {
                if (userAgent.Contains("MSIE 6.") || userAgent.Contains("MSIE 7.") || userAgent.Contains("MSIE 8."))
                {
                    f = false;
                }
            }
        }
        return f;
    }
}
