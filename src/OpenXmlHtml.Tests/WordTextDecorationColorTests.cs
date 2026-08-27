[TestFixture]
public class WordTextDecorationColorTests
{
    [Test]
    public Task UnderlineColor() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-decoration: underline; text-decoration-color: red">red underline</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:u w:val=\"single\" w:color=\"FF0000\" /></w:rPr><w:t xml:space=\"preserve\">red underline</w:t></w:r></w:p>");

    [Test]
    public Task UnderlineColorHex() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-decoration: underline; text-decoration-color: #00FF00">green underline</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:u w:val=\"single\" w:color=\"00FF00\" /></w:rPr><w:t xml:space=\"preserve\">green underline</w:t></w:r></w:p>");

    [Test]
    public Task DottedColoredUnderline() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-decoration: underline; text-decoration-style: dotted; text-decoration-color: blue">blue dotted</span>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:u w:val=\"dotted\" w:color=\"0000FF\" /></w:rPr><w:t xml:space=\"preserve\">blue dotted</w:t></w:r></w:p>");
}
