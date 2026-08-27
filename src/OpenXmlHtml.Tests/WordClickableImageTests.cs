[TestFixture]
public class WordClickableImageTests
{
    [Test]
    public Task ImageInsideLink()
    {
        var png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            $"""<p><a href="https://example.com"><img src="data:image/png;base64,{png}" width="50" height="50"></a></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task ImageAndTextInsideLink()
    {
        var png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            $"""<p><a href="https://example.com"><img src="data:image/png;base64,{png}" width="30" height="30"> Visit site</a></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task ExternalLinkCreatesHyperlink()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p>Click <a href="https://example.com">here</a> for details.</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task ExternalLinkFallbackWithoutMainPart()
    {
        var elements = WordHtmlConverter.ToElements(
            """<p>Click <a href="https://example.com">here</a> for details.</p>""");
        return Verify(elements)
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Click </w:t></w:r><w:r><w:rPr><w:color w:val=\"0563C1\" /><w:u w:val=\"single\" /></w:rPr><w:t xml:space=\"preserve\">here</w:t></w:r><w:r><w:t xml:space=\"preserve\"> (https://example.com)</w:t></w:r><w:r><w:t xml:space=\"preserve\"> for details.</w:t></w:r></w:p>");
    }
}
