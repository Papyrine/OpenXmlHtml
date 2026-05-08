[TestFixture]
public class WordImageFloatTests
{
    const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";

    static List<OpenXmlElement> Build(string html)
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        return WordHtmlConverter.ToElements(html, main);
    }

    [Test]
    public Task FloatLeft() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="float: left; width: 100px; height: 100px"> wrapped text</p>"""));

    [Test]
    public Task FloatRight() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="float: right; width: 100px; height: 100px"> wrapped text</p>"""));

    [Test]
    public Task FloatNone() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="float: none; width: 100px"> inline</p>"""));

    [Test]
    public Task FloatLeftSvg() =>
        Verify(Build("""<p><svg style="float: left" width="50" height="50"><rect width="50" height="50"/></svg> next to svg</p>"""));

    [Test]
    public Task FloatLeftDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            $"""<p><img src="data:image/png;base64,{png}" style="float: left; width: 80px; height: 80px"> body text after image</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
