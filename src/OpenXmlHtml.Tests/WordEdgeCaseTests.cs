[TestFixture]
public class WordEdgeCaseTests
{
    [Test]
    public Task UnclosedTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>italic"));

    [Test]
    public Task ConsecutiveBreaks() =>
        Verify(WordHtmlConverter.ToParagraphs("one<br><br><br>two"));

    [Test]
    public Task LineBreakInElements() =>
        Verify(WordHtmlConverter.ToElements("<p>a<br>b</p>"));

    [Test]
    public Task LineBreakInListItem() =>
        Verify(WordHtmlConverter.ToElements("<ul><li>a<br>b</li><li>c</li></ul>"));

    [Test]
    public Task HorizontalRuleStillBreaksParagraph() =>
        Verify(WordHtmlConverter.ToParagraphs("one<hr>two"));


    [Test]
    public Task WhitespaceCollapsing() =>
        Verify(WordHtmlConverter.ToParagraphs("  lots   of    spaces  "));

    [Test]
    public Task WhitespaceFoldsWithinTextNode() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>Line1\r\n\r\nLine2</p>"));

    // Each text node collapses on its own, so without carrying the folding state across the inline
    // boundaries this rendered as "a  x  y   z".
    [Test]
    public Task WhitespaceFoldsAcrossInlineNodes() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>a <b> x</b> <i>y </i> z</p>"));

    [Test]
    public Task SpecialCharacters() =>
        Verify(WordHtmlConverter.ToParagraphs("price: $100 &amp; tax &lt; 10%"));

    [Test]
    public Task UnknownTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<custom>text</custom>"));

    [Test]
    public Task ImageAlt() =>
        Verify(WordHtmlConverter.ToParagraphs("before <img alt=\"image description\"> after"));

    [Test]
    public Task EmptyTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<b></b><i></i>text"));

    [Test]
    public Task MalformedHtml() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>overlap</b> still italic</i>"));

    [Test]
    public Task CiteTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<cite>citation</cite>"));

    [Test]
    public Task VarTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<var>variable</var>"));

    [Test]
    public Task InvalidXmlCharsFromEntities() =>
        Verify(WordHtmlConverter.ToParagraphs("before&#1;&#0;&#x1F;after"));

    [Test]
    public Task InvalidXmlCharsRaw() =>
        Verify(WordHtmlConverter.ToParagraphs("before\u0001\u0000\u001fafter"));

    [Test]
    public Task LoneSurrogate() =>
        Verify(WordHtmlConverter.ToParagraphs("before\uD800after"));

}
