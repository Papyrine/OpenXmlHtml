[TestFixture]
public class WordWordSpacingTests
{
    [Test]
    public Task WordSpacingPx() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="word-spacing: 5px">spaced words</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:spacing w:val=\"75\" /></w:rPr><w:t xml:space=\"preserve\">spaced words</w:t></w:r></w:p>");

    [Test]
    public Task WordSpacingPt() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="word-spacing: 3pt">spaced words</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:spacing w:val=\"60\" /></w:rPr><w:t xml:space=\"preserve\">spaced words</w:t></w:r></w:p>");

    [Test]
    public Task WordSpacingNormal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="word-spacing: normal">normal</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">normal</w:t></w:r></w:p>");
}
