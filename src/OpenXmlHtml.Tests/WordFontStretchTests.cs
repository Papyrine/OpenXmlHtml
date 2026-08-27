[TestFixture]
public class WordFontStretchTests
{
    [Test]
    public Task Condensed() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: condensed">condensed</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:w w:val=\"75\" /></w:rPr><w:t xml:space=\"preserve\">condensed</w:t></w:r></w:p>");

    [Test]
    public Task Expanded() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: expanded">expanded</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:w w:val=\"125\" /></w:rPr><w:t xml:space=\"preserve\">expanded</w:t></w:r></w:p>");

    [Test]
    public Task UltraCondensed() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: ultra-condensed">tight</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:w w:val=\"50\" /></w:rPr><w:t xml:space=\"preserve\">tight</w:t></w:r></w:p>");

    [Test]
    public Task PercentValue() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: 80%">scaled</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:w w:val=\"80\" /></w:rPr><w:t xml:space=\"preserve\">scaled</w:t></w:r></w:p>");

    [Test]
    public Task Normal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: normal">normal</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:w w:val=\"100\" /></w:rPr><w:t xml:space=\"preserve\">normal</w:t></w:r></w:p>");
}
