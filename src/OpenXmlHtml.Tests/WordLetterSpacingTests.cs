[TestFixture]
public class WordLetterSpacingTests
{
    [Test]
    public Task LetterSpacingPx() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p><span style="letter-spacing: 4px">spaced out</span></p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:spacing w:val=\"60\" /></w:rPr><w:t xml:space=\"preserve\">spaced out</w:t></w:r></w:p>");

    [Test]
    public Task LetterSpacingPt() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p><span style="letter-spacing: 2pt">two point spacing</span></p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:spacing w:val=\"40\" /></w:rPr><w:t xml:space=\"preserve\">two point spacing</w:t></w:r></w:p>");

    [Test]
    public Task LetterSpacingNormal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p><span style="letter-spacing: normal">default spacing</span></p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">default spacing</w:t></w:r></w:p>");

    [Test]
    public Task LetterSpacingConvertToDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p><span style="letter-spacing: 3px">spread chars</span></p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
