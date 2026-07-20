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

    // Heading3 carries Bold in its style run properties, so ", John" needs an explicit
    // <w:b w:val="0"/> to render unbolded — omitting <w:b/> would inherit the style's bold.
    [Test]
    public Task FontWeightNormalOverridesBoldHeadingStyle() =>
        Verify(WordHtmlConverter.ToElements(
            """<h3><b>SMITH</b><span style="font-weight: normal">, John</span></h3>"""));

    // font-variant inherits, so the inner span has to emit an explicit off. Left unset it inherits
    // the enclosing small-caps and renders identically to the text around it.
    [Test]
    public Task FontVariantNormalResetsSmallCaps() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="font-variant: small-caps">caps <span style="font-variant: normal">not caps</span></span>"""));

    [Test]
    public Task FontVariantSmallCaps() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-variant: small-caps">caps</span>"""));

    // A run whose only styling is the reset still needs an rPr to carry it.
    [Test]
    public Task FontVariantNormalAloneStillEmitsRunProperties() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="font-variant: normal">plain</span>"""));

    // text-shadow inherits as well, so `none` cancels rather than merely declining to add.
    [Test]
    public Task TextShadowNoneResetsShadow() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<span style="text-shadow: 1px 1px">shadowed <span style="text-shadow: none">not shadowed</span></span>"""));

    [Test]
    public Task TextShadow() =>
        Verify(WordHtmlConverter.ToParagraphs("""<span style="text-shadow: 1px 1px">shadowed</span>"""));
}
