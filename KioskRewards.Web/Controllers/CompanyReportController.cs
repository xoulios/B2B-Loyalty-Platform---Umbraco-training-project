using System.Text;
using KioskRewards.Application.Abstractions;
using KioskRewards.Application.DTOs;
using KioskRewards.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KioskRewards.Web.Controllers;

/// <summary>
/// Route-hijacks the companyReport page (Public Access restricted to the "Company Admin" member
/// group) to show aggregate loyalty stats. ?format=csv on the same URL returns the same numbers as a
/// CSV download instead of the HTML view - same protected route, no extra plumbing needed.
/// </summary>
public sealed class CompanyReportController : RenderController
{
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly ServiceContext _serviceContext;
    private readonly IReportingService _reportingService;

    public CompanyReportController(
        ILogger<CompanyReportController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IVariationContextAccessor variationContextAccessor,
        ServiceContext serviceContext,
        IReportingService reportingService)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _variationContextAccessor = variationContextAccessor;
        _serviceContext = serviceContext;
        _reportingService = reportingService;
    }

    public override IActionResult Index() => IndexAsync().GetAwaiter().GetResult();

    private async Task<IActionResult> IndexAsync()
    {
        var report = await _reportingService.GetReportAsync();

        if (string.Equals(Request.Query["format"], "csv", StringComparison.OrdinalIgnoreCase))
            return File(Encoding.UTF8.GetBytes(BuildCsv(report)), "text/csv", "company-report.csv");

        var kioskNames = report.TopKiosks.ToDictionary(
            k => k.MemberKey,
            k => _serviceContext.MemberService.GetByKey(k.MemberKey)?.Name ?? k.MemberKey.ToString());

        var fallback = new PublishedValueFallback(_serviceContext, _variationContextAccessor);
        var model = new CompanyReportViewModel(CurrentPage!, fallback) { Report = report, KioskNames = kioskNames };

        return CurrentTemplate(model);
    }

    private static string BuildCsv(CompanyReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Metric,Value");
        sb.AppendLine($"Total points earned,{report.TotalPointsEarned}");
        sb.AppendLine($"Total points redeemed,{report.TotalPointsRedeemed}");
        sb.AppendLine($"Net points outstanding,{report.NetPointsOutstanding}");
        sb.AppendLine();
        sb.AppendLine("Top reward,Redemption count,Total points spent");
        foreach (var reward in report.TopRewards)
            sb.AppendLine($"{CsvEscape(reward.Description)},{reward.RedemptionCount},{reward.TotalPointsSpent}");
        sb.AppendLine();
        sb.AppendLine("Top kiosk (member key),Total points earned,Total points redeemed");
        foreach (var kiosk in report.TopKiosks)
            sb.AppendLine($"{kiosk.MemberKey},{kiosk.TotalPointsEarned},{kiosk.TotalPointsRedeemed}");
        return sb.ToString();
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
