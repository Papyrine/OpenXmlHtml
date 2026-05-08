[TestFixture]
public class WordWordSpacingTests
{
    [Test]
    public Task WordSpacingPx() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="word-spacing: 5px">spaced words</span>"""));

    [Test]
    public Task WordSpacingPt() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="word-spacing: 3pt">spaced words</span>"""));

    [Test]
    public Task WordSpacingNormal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="word-spacing: normal">normal</span>"""));
}
