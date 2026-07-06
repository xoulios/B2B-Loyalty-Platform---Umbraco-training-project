using Umbraco.Cms.Core.Dictionary;

namespace KioskRewards.Web;

/// <summary>
/// ICultureDictionary's indexer returns "" (empty string, NOT null) for a key that doesn't exist yet
/// in the backoffice - so the "dictionary[key] ?? fallback" pattern looks right but silently never
/// falls back for a truly missing key (an empty string isn't null). This is the one place that gets
/// it right, for every controller to share instead of repeating the same mistake.
/// </summary>
internal static class CultureDictionaryExtensions
{
    public static string GetValueOrFallback(this ICultureDictionary dictionary, string key, string fallback)
    {
        var value = dictionary[key];
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}
