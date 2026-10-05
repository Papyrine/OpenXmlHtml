#if NETFRAMEWORK
using System.Net.Http;
#endif

public class WordImageFallbackTests
{
    [Test]
    public async Task InvalidBase64DataUri_FallsBackToAlt()
    {
        var elements = WordHtmlConverter.ToElements(
            """<p><img src="data:image/png;base64,!!!notbase64!!!" alt="Bad"></p>""");

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "Bad");
    }

    [Test]
    public async Task NonBase64DataUri_FallsBackToAlt()
    {
        var elements = WordHtmlConverter.ToElements(
            """<p><img src="data:image/png,raw" alt="Bad"></p>""");

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "Bad");
    }

    [Test]
    public Task ImageWithoutMainPart_SilentlyDropped()
    {
        var png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
        var elements = WordHtmlConverter.ToElements(
            $"""<p><img src="data:image/png;base64,{png}"></p>""");

        return AssertNoDrawing(elements);
    }

    [Test]
    public async Task HttpThrows_FallsBackToAlt()
    {
        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.AllowAll(),
            HttpClient = new(new ThrowingHandler())
        };

        var elements = WordHtmlConverter.ToElements(
            """<p><img src="https://example.com/img.png" alt="NoNet"></p>""",
            null,
            settings);

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "NoNet");
    }

    [Test]
    public async Task HttpNotSuccess_FallsBackToAlt()
    {
        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.AllowAll(),
            HttpClient = new(new StatusCodeHandler(System.Net.HttpStatusCode.InternalServerError))
        };

        var elements = WordHtmlConverter.ToElements(
            """<p><img src="https://example.com/img.png" alt="500"></p>""",
            null,
            settings);

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "500");
    }

    [Test]
    public async Task LocalImageMissing_FallsBackToAlt()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "definitely_missing_" + Guid.NewGuid().ToString("N") + ".png");
        var settings = new HtmlConvertSettings
        {
            LocalImages = ImagePolicy.AllowAll()
        };

        var elements = WordHtmlConverter.ToElements(
            $"""<p><img src="{missingPath.Replace("\\", "/")}" alt="Missing"></p>""",
            null,
            settings);

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "Missing");
    }

    [Test]
    public async Task LocalImageInvalidPath_FallsBackToAlt()
    {
        var settings = new HtmlConvertSettings
        {
            LocalImages = ImagePolicy.AllowAll()
        };

        var elements = WordHtmlConverter.ToElements(
            """<p><img src="\0invalid" alt="Invalid"></p>""",
            null,
            settings);

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "Invalid");
    }

    [Test]
    public async Task LocalImageMalformedFileUri_FallsBackToAlt()
    {
        var settings = new HtmlConvertSettings
        {
            LocalImages = ImagePolicy.AllowAll()
        };

        var elements = WordHtmlConverter.ToElements(
            """<p><img src="file:///%" alt="Malformed"></p>""",
            null,
            settings);

        await AssertNoDrawing(elements);
        await AssertContainsText(elements, "Malformed");
    }

    static async Task AssertNoDrawing(List<OpenXmlElement> elements)
    {
        var hasDrawing = elements.Any(_ => _.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any());
        await Assert.That(hasDrawing).IsFalse().Because("Expected no Drawing elements");
    }

    static async Task AssertContainsText(List<OpenXmlElement> elements, string text)
    {
        var combined = string.Concat(elements.SelectMany(_ => _.Descendants<WText>().Select(_ => _.Text)));
        await Assert.That(combined).Contains(text);
    }

    class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Cancel cancel) =>
            throw new HttpRequestException("boom");
    }

    class StatusCodeHandler(System.Net.HttpStatusCode code) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Cancel cancel) =>
            Task.FromResult(new HttpResponseMessage(code));
    }
}
