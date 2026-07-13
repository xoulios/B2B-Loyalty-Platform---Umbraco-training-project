using KioskRewards.Application.DTOs;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace KioskRewards.Web.Models;

/// <summary>
/// Same pattern as MemberDashboardViewModel - extends the generated (property-less) CompanyReport
/// class so the view still gets Model.Name/Model.Url() for free, plus the aggregate report bolted on.
/// </summary>
public sealed class CompanyReportViewModel : CompanyReport
{
    // Umbraco's PublishedModelFactory needs this exact constructor shape on every subclass of a
    // generated model, so extra data has to be settable properties instead of extra ctor params.
    public CompanyReportViewModel(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
        : base(content, publishedValueFallback)
    {
    }

    public CompanyReportDto Report { get; init; } = null!;
}
