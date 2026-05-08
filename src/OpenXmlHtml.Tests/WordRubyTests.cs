[TestFixture]
public class WordRubyTests
{
    [Test]
    public Task BasicRuby() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<ruby>漢<rt>kan</rt></ruby><ruby>字<rt>ji</rt></ruby>"));

    [Test]
    public Task RubyWithRpFallback() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<ruby>漢<rp>(</rp><rt>kan</rt><rp>)</rp></ruby>"));

    [Test]
    public Task RubyInParagraph() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>The <ruby>kanji<rt>annotation</rt></ruby> word.</p>"));
}
