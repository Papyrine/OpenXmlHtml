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

    [Test]
    public Task FontWeightNormalResetsBold() =>
        Verify(WordHtmlConverter.ToParagraphs("""<b>bold <span style="font-weight: normal">not bold</span></b>"""));

    [Test]
    public Task FontWeightLighterResetsBold() =>
        Verify(WordHtmlConverter.ToParagraphs("""<b>bold <span style="font-weight: lighter">not bold</span></b>"""));

    [Test]
    public Task FontWeight300ResetsBold() =>
        Verify(WordHtmlConverter.ToParagraphs("""<b>bold <span style="font-weight: 300">not bold</span></b>"""));

    [Test]
    public Task FontWeightUppercaseBold() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-weight: BOLD">bold</span>"""));

    [Test]
    public Task FontStyleNormalResetsItalic() =>
        Verify(WordHtmlConverter.ToParagraphs("""<i>italic <span style="font-style: normal">not italic</span></i>"""));

    [Test]
    public Task FontStyleUppercaseItalic() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-style: ITALIC">italic</span>"""));
}
