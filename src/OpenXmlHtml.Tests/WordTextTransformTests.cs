[TestFixture]
public class WordTextTransformTests
{
    [Test]
    public Task Uppercase()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><span style="text-transform: uppercase">this should be uppercase</span></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task Lowercase()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><span style="text-transform: lowercase">THIS SHOULD BE LOWERCASE</span></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task Capitalize()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><span style="text-transform: capitalize">each word should be capitalized</span></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task UppercaseViaToParagraphs()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            """<p><span style="text-transform: uppercase">also works in flat path</span></p>""");
        return Verify(paragraphs)
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">ALSO WORKS IN FLAT PATH</w:t></w:r></w:p>");
    }
}
