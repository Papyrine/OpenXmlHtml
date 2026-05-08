[TestFixture]
public class WordTextDecorationColorTests
{
    [Test]
    public Task UnderlineColor() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-decoration: underline; text-decoration-color: red">red underline</span>"""));

    [Test]
    public Task UnderlineColorHex() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-decoration: underline; text-decoration-color: #00FF00">green underline</span>"""));

    [Test]
    public Task DottedColoredUnderline() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-decoration: underline; text-decoration-style: dotted; text-decoration-color: blue">blue dotted</span>"""));
}
