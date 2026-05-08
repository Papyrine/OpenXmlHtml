[TestFixture]
public class WordFontWeightTests
{
    [Test]
    public Task FontWeight400() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: 400">normal</span>"""));

    [Test]
    public Task FontWeight500() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: 500">medium</span>"""));

    [Test]
    public Task FontWeight600() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: 600">semibold</span>"""));

    [Test]
    public Task FontWeight700() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: 700">bold</span>"""));

    [Test]
    public Task FontWeight900() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: 900">black</span>"""));

    [Test]
    public Task FontWeightBolder() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: bolder">bolder</span>"""));
}
