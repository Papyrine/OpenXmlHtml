[TestFixture]
public class WordTextShadowTests
{
    [Test]
    public Task TextShadow() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p><span style="text-shadow: 1px 1px 2px black">shadowed text</span></p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:shadow /></w:rPr><w:t xml:space=\"preserve\">shadowed text</w:t></w:r></w:p>");

    [Test]
    public Task TextShadowNone() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p><span style="text-shadow: none">no shadow</span></p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:shadow w:val=\"false\" /></w:rPr><w:t xml:space=\"preserve\">no shadow</w:t></w:r></w:p>");

    [Test]
    public Task TextShadowConvertToDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><span style="text-shadow: 2px 2px 4px gray">shadow run</span></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
