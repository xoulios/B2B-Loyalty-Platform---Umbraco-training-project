using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace KioskRewards.Web.Controllers;

/// <summary>
/// Plain MVC controller (not Surface/Render) - sitemap.xml and robots.txt aren't tied to any single
/// content node, so there's nothing to hijack; a normal attribute-routed action is the right tool.
/// </summary>
public sealed class SitemapController : Controller
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    private readonly IPublishedContentQuery _contentQuery;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly IPublicAccessService _publicAccessService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly IVariationContextAccessor _variationContextAccessor;

    public SitemapController(
        IPublishedContentQuery contentQuery,
        IPublishedUrlProvider urlProvider,
        IPublicAccessService publicAccessService,
        IUmbracoContextFactory umbracoContextFactory,
        IVariationContextAccessor variationContextAccessor)
    {
        _contentQuery = contentQuery;
        _urlProvider = urlProvider;
        _publicAccessService = publicAccessService;
        _umbracoContextFactory = umbracoContextFactory;
        _variationContextAccessor = variationContextAccessor;
    }

    [HttpGet("sitemap.xml")]
    public IActionResult Sitemap()
    {
        // Plain attribute-routed controllers run outside Umbraco's own front-end request pipeline, so
        // neither UmbracoContext nor VariationContext are set up ambiently here (unlike Surface/Render
        // controllers). Without an explicit VariationContext, culture-filtered tree traversal
        // (.Children()) becomes unreliable - it ends up depending on whatever culture happened to be
        // left on the current thread-pool thread by an earlier, unrelated request.
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        _variationContextAccessor.VariationContext = new VariationContext("el-GR");

        var urlElements = _contentQuery.ContentAtRoot()
            .SelectMany(AllNodes)
            .Where(IsSitemapEligible)
            .SelectMany(BuildUrlElements);

        var document = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(SitemapNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", XhtmlNs.NamespaceName),
                urlElements));

        using var writer = new Utf8StringWriter();
        document.Save(writer);
        return Content(writer.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var body = $"""
            User-agent: *
            Disallow: /umbraco/

            Sitemap: {Request.Scheme}://{Request.Host}/sitemap.xml
            """;
        return Content(body, "text/plain", Encoding.UTF8);
    }

    private IEnumerable<XElement> BuildUrlElements(IPublishedContent node)
    {
        var culturesWithUrls = node.Cultures.Keys
            .OrderBy(culture => culture, StringComparer.OrdinalIgnoreCase)
            .Select(culture => (Culture: culture, Url: node.Url(_urlProvider, culture, UrlMode.Absolute)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url) && x.Url != "#")
            .ToList();

        foreach (var (_, url) in culturesWithUrls)
        {
            yield return new XElement(SitemapNs + "url",
                new XElement(SitemapNs + "loc", url),
                new XElement(SitemapNs + "lastmod", node.UpdateDate.ToString("yyyy-MM-dd")),
                culturesWithUrls.Select(alternate => new XElement(XhtmlNs + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", alternate.Culture),
                    new XAttribute("href", alternate.Url))));
        }
    }

    private bool IsSitemapEligible(IPublishedContent node) =>
        node.TemplateId is not null
        // loginPage is publicly reachable (anonymous users need it to sign in) but has no SEO value -
        // same reasoning that already excludes it from the public nav in _Layout.cshtml.
        && node.ContentType.Alias is not "loginPage"
        && !_publicAccessService.IsProtected(node.Path).Success;

    private static IEnumerable<IPublishedContent> AllNodes(IPublishedContent node)
    {
        yield return node;
        foreach (var child in node.Children())
        foreach (var descendant in AllNodes(child))
            yield return descendant;
    }

    // StringWriter defaults its Encoding to UTF-16 (it's char-based), which would make XDocument
    // write a mismatched "encoding=utf-16" declaration even though Content() below serves UTF-8 bytes.
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
