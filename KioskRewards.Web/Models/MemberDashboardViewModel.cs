using KioskRewards.Application.DTOs;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace KioskRewards.Web.Models;

/// <summary>
/// Dashboard view model - extends the generated MemberDashboard class instead of wrapping it, so the
/// view still gets Model.Name/Model.Url() etc for free, plus the loyalty data bolted on.
/// </summary>
public sealed class MemberDashboardViewModel : MemberDashboard
{
    // Umbraco's PublishedModelFactory needs this exact constructor shape on every subclass of a
    // generated model, so extra data has to be settable properties instead of extra ctor params.
    public MemberDashboardViewModel(IPublishedContent content, IPublishedValueFallback publishedValueFallback)
        : base(content, publishedValueFallback)
    {
    }

    public int Balance { get; init; }
    public IReadOnlyList<PointsTransactionDto> History { get; init; } = Array.Empty<PointsTransactionDto>();
}
