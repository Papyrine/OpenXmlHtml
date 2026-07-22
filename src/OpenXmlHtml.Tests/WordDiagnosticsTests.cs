#if NETFRAMEWORK
using System.Net.Http;
#endif

[TestFixture]
public class WordDiagnosticsTests
{
    const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";

    // Collects what a conversion discarded. Runs through ToElements with a MainDocumentPart, so
    // nothing is reported merely for the part being absent.
    static List<HtmlDiagnostic> Collect(string html)
    {
        var diagnostics = new List<HtmlDiagnostic>();
        WordHtmlConverter.ToElements(
            html,
            NewMainPart(),
            new()
            {
                OnDiagnostic = diagnostics.Add
            });
        return diagnostics;
    }

    static MainDocumentPart NewMainPart()
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        return document.AddMainDocumentPart();
    }

    [Test]
    public Task ColPercentageWidthAttribute() =>
        Verify(Collect(
            """
            <table>
              <col width="50%">
              <col width="50%">
              <tr><td>A</td><td>B</td></tr>
            </table>
            """));

    [Test]
    public Task ColPercentageWidthCss() =>
        Verify(Collect(
            """
            <table>
              <col style="width: 50%">
              <tr><td>A</td></tr>
            </table>
            """));

    // The same percentage on a cell or on the table reaches Word as w:type="pct", so it is not a
    // drop and says nothing.
    [Test]
    public Task CellAndTablePercentageWidthsAreNotDropped() =>
        Verify(Collect(
            """
            <table style="width: 80%">
              <tr><td width="35%">A</td><td style="width: 65%">B</td></tr>
            </table>
            """));

    [Test]
    public Task ImagePercentageWidthCss() =>
        Verify(Collect($"""<p><img src="data:image/png;base64,{png}" style="width: 50%"></p>"""));

    [Test]
    public Task ImagePercentageWidthAttribute() =>
        Verify(Collect($"""<p><img src="data:image/png;base64,{png}" width="50%"></p>"""));

    // The attribute supplies a width, so the output is not obviously broken — but it is not the 50%
    // that was asked for either, which is the kind of drop worth hearing about.
    [Test]
    public Task ImagePercentageWidthFallingBackToAttribute() =>
        Verify(Collect($"""<p><img src="data:image/png;base64,{png}" style="width: 50%" width="150"></p>"""));

    [Test]
    public Task UnsupportedElements() =>
        Verify(Collect(
            """
            <p>Before</p>
            <video src="clip.mp4"></video>
            <iframe src="https://example.com"></iframe>
            <canvas></canvas>
            <p>After</p>
            """));

    // Non-rendered metadata is not a drop, and neither is markup the author hid on purpose:
    // a browser draws nothing for any of it either.
    [Test]
    public Task MetadataAndHiddenElementsAreNotReported() =>
        Verify(Collect(
            """
            <style>p { color: red }</style>
            <script>alert(1)</script>
            <p hidden>Hidden</p>
            <p style="display: none">Gone</p>
            <p>Kept</p>
            """));

    [Test]
    public Task WebImageBlockedByPolicy() =>
        Verify(Collect("""<p><img src="https://cdn.example.com/logo.png" alt="Logo"></p>"""));

    [Test]
    public Task LocalImageBlockedByPolicy() =>
        Verify(Collect("""<p><img src="logo.png" alt="Logo"></p>"""));

    [Test]
    public Task WebImageAllowedButUnreachable()
    {
        var diagnostics = new List<HtmlDiagnostic>();
        WordHtmlConverter.ToElements(
            """<p><img src="https://cdn.example.com/logo.png"></p>""",
            NewMainPart(),
            new()
            {
                WebImages = ImagePolicy.SafeDomains("cdn.example.com"),
                HttpClient = new(new FailingHandler()),
                OnDiagnostic = diagnostics.Add
            });
        return Verify(diagnostics);
    }

    [Test]
    public Task MalformedDataUri() =>
        Verify(Collect("""<p><img src="data:image/png;base64,not!valid!base64"></p>"""));

    [Test]
    public Task DataUriWithoutBase64Marker() =>
        Verify(Collect("""<p><img src="data:image/png,raw"></p>"""));

    // Converting without a MainDocumentPart resolves the image and then has nowhere to put it.
    [Test]
    public Task ImageWithoutMainPart()
    {
        var diagnostics = new List<HtmlDiagnostic>();
        WordHtmlConverter.ToElements(
            ImageHtml,
            null,
            new()
            {
                OnDiagnostic = diagnostics.Add
            });
        return Verify(diagnostics);
    }

    // The flat segment path loses the same two images for the same reason, so it says the same
    // thing — including which tag each came from, which the segment list otherwise erases.
    [Test]
    public Task ImageWithoutMainPartSegmentPath()
    {
        var diagnostics = new List<HtmlDiagnostic>();
        WordHtmlConverter.ToParagraphs(
            ImageHtml,
            null,
            new()
            {
                OnDiagnostic = diagnostics.Add
            });
        return Verify(diagnostics);
    }

    static string ImageHtml =>
        $"""<p><img src="data:image/png;base64,{png}"></p><p><svg width="10" height="10"></svg></p>""";

    // The two paths share the resolver and the element skip list, so markup exercising both reports
    // the same drops through ToElements and ToParagraphs.
    [Test]
    public Task BothPathsAgree()
    {
        const string html =
            """
            <p><img src="https://cdn.example.com/logo.png" style="width: 50%"></p>
            <iframe src="https://example.com"></iframe>
            """;

        // Same MainDocumentPart on both, so the only difference is which builder walked the dom.
        var main = NewMainPart();

        var elementPath = new List<HtmlDiagnostic>();
        WordHtmlConverter.ToElements(
            html,
            main,
            new()
            {
                OnDiagnostic = elementPath.Add
            });

        var segmentPath = new List<HtmlDiagnostic>();
        WordHtmlConverter.ToParagraphs(
            html,
            main,
            new()
            {
                OnDiagnostic = segmentPath.Add
            });

        Assert.That(segmentPath, Is.EqualTo(elementPath));
        return Verify(elementPath);
    }

    // The test-time use the sink exists for: markup believed fully supported reports nothing.
    [Test]
    public Task NoneForFullySupportedMarkup() =>
        Verify(Collect(
            $"""
            <h1 id="top">Report</h1>
            <p style="text-align: center; margin-top: 12pt">Body with <b>bold</b> and <a href="https://example.com">a link</a>.</p>
            <ul><li>One</li><li>Two</li></ul>
            <table style="width: 80%">
              <col style="width: 100px">
              <tr><th>Head</th></tr>
              <tr><td style="background-color: #eee">Cell</td></tr>
            </table>
            <p><img src="data:image/png;base64,{png}" width="64" height="64"></p>
            """));

    // Unsubscribed is the default: the drop sites still run, they just have nowhere to report to.
    [Test]
    public void SilentWithoutSink()
    {
        var elements = WordHtmlConverter.ToElements(
            """
            <table>
              <col width="50%">
              <tr><td>A</td></tr>
            </table>
            """);
        Assert.That(elements, Is.Not.Empty);
    }

    class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            Cancel cancel) =>
            throw new HttpRequestException("no such host");
    }
}
