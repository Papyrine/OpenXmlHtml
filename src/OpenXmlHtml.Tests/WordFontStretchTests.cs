[TestFixture]
public class WordFontStretchTests
{
    [Test]
    public Task Condensed() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: condensed">condensed</span>"""));

    [Test]
    public Task Expanded() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: expanded">expanded</span>"""));

    [Test]
    public Task UltraCondensed() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: ultra-condensed">tight</span>"""));

    [Test]
    public Task PercentValue() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: 80%">scaled</span>"""));

    [Test]
    public Task Normal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-stretch: normal">normal</span>"""));
}
