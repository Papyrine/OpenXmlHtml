#if NETFRAMEWORK
using System.Net.Http;
#endif

public class WordRemoteImageTests
{
    // 2x2 red PNG
    public static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==");

    static HttpClient CreateFakeClient() =>
        new(new FakeImageHandler());

    [Test]
    public Task WebImage_AllowAll()
    {
        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.AllowAll(),
            HttpClient = CreateFakeClient()
        };

        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><img src="https://example.com/logo.png" width="50" height="50"></p>""",
            stream,
            settings);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task WebImage_SafeDomains_Allowed()
    {
        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.SafeDomains("example.com"),
            HttpClient = CreateFakeClient()
        };

        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><img src="https://cdn.example.com/logo.png" width="50" height="50"></p>""",
            stream,
            settings);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task WebImage_SafeDomains_Denied()
    {
        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.SafeDomains("trusted.com"),
            HttpClient = CreateFakeClient()
        };

        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><img src="https://evil.com/logo.png" alt="Blocked Image"></p>""",
            stream,
            settings);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task WebImage_Filter()
    {
        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.Filter(src => src.Contains("allowed")),
            HttpClient = CreateFakeClient()
        };

        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><img src="https://allowed.example.com/logo.png" width="50" height="50"></p>""",
            stream,
            settings);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public async Task LocalImage_SafeDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "OpenXmlHtmlTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            var imagePath = Path.Combine(tempDir, "test.png");
            await File.WriteAllBytesAsync(imagePath, PngBytes);

            var settings = new HtmlConvertSettings
            {
                LocalImages = ImagePolicy.SafeDirectories(tempDir)
            };

            using var stream = new MemoryStream();
            WordHtmlConverter.ConvertToDocx(
                $"""<p><img src="{imagePath.Replace("\\", "/")}" width="50" height="50"></p>""",
                stream,
                settings);
            stream.Position = 0;
            await Verify(stream, "docx");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public Task LocalImage_Denied_FallsBackToAlt()
    {
        var settings = new HtmlConvertSettings
        {
            LocalImages = ImagePolicy.Deny()
        };

        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><img src="C:\Images\photo.png" alt="Photo"></p>""",
            stream,
            settings);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    class FakeImageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            Cancel cancel)
        {
            var content = new ByteArrayContent(PngBytes);
            content.Headers.ContentType = new("image/png");
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = content
            };
            return Task.FromResult(response);
        }
    }

    // Where the output cannot hold an image, nothing is fetched or decoded for it. The filter is
    // the witness: it is only consulted when an image is about to be resolved.
    [Test]
    public async Task ImagesAreNotResolvedWhereTheyCannotBePlaced()
    {
        var asked = new List<string>();
        var settings = new HtmlConvertSettings
        {
            LocalImages = ImagePolicy.Filter(source =>
            {
                asked.Add(source);
                return false;
            })
        };
        const string html = """<p>before <img src="picture.png" alt="Logo"> after</p>""";

        var cell = SpreadsheetHtmlConverter.ToInlineString(html, settings);
        var paragraphs = WordHtmlConverter.ToParagraphs(html, null, settings);
        var elements = WordHtmlConverter.ToElements(html, null, settings);

        await Assert.That(asked).IsEmpty();
        await Assert.That(cell.InnerText).IsEqualTo("before Logo after");
        await Assert.That(string.Concat(paragraphs.Select(_ => _.InnerText))).IsEqualTo("before Logo after");
        await Assert.That(string.Concat(elements.Select(_ => _.InnerText))).Contains("Logo");
    }

    // With a document to put it in, the image is resolved as before.
    [Test]
    public async Task ImagesAreStillResolvedWhereTheyCanBePlaced()
    {
        var asked = new List<string>();
        var settings = new HtmlConvertSettings
        {
            LocalImages = ImagePolicy.Filter(source =>
            {
                asked.Add(source);
                return false;
            })
        };
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();

        WordHtmlConverter.ToElements("""<img src="one.png">""", main, settings);
        WordHtmlConverter.ToParagraphs("""<img src="two.png">""", main, settings);

        await Assert.That(asked).IsEquivalentTo(["one.png", "two.png"], CollectionOrdering.Matching);
    }
}
