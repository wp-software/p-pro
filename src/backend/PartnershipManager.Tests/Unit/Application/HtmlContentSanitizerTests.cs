using PartnershipManager.Application.Common.Security;

namespace PartnershipManager.Tests.Unit.Application;

public class HtmlContentSanitizerTests
{
    [Fact]
    public void Sanitize_RemovesScriptsEventHandlersAndJavascriptUrls()
    {
        var result = HtmlContentSanitizer.Sanitize(
            "<p onclick=\"run()\">safe</p><img src=x onerror=run()><a href=\"javascript:run()\">link</a><script>run()</script>");

        result.Should().Contain("<p>safe</p>");
        result.Should().Contain("link</a>");
        result.Should().NotContain("<script");
        result.Should().NotContain("onclick");
        result.Should().NotContain("onerror");
        result.Should().NotContain("javascript:");
    }
}
