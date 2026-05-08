[TestFixture]
public class WordPageBreakTests
{
    [Test]
    public Task PageBreakBefore()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p>Page one</p><p style="page-break-before: always">Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task PageBreakAfter()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p style="page-break-after: always">Page one</p><p>Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task BreakBeforePage()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p>Page one</p><p style="break-before: page">Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task BreakAfterPage()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p style="break-after: page">Page one</p><p>Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
