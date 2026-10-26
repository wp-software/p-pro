using Ganss.Xss;

namespace PartnershipManager.Application.Common.Security;

public static class HtmlContentSanitizer
{
    private static readonly string[] AllowedTags =
    [
        "a", "b", "blockquote", "br", "caption", "code", "dd", "div", "dl", "dt",
        "em", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "li", "ol", "p",
        "pre", "span", "strong", "sub", "sup", "table", "tbody", "td", "th",
        "thead", "tr", "u", "ul"
    ];

    private static readonly string[] AllowedAttributes =
    [
        "class", "colspan", "href", "rowspan", "title"
    ];

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedSchemes.Clear();

        foreach (var tag in AllowedTags)
        {
            sanitizer.AllowedTags.Add(tag);
        }

        foreach (var attribute in AllowedAttributes)
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }

        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("tel");

        return sanitizer.Sanitize(html);
    }
}
