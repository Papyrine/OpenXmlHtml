[TestFixture]
public class WordEdgeCaseTests
{
    [Test]
    public Task UnclosedTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>italic"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">bold </w:t></w:r><w:r><w:rPr><w:b /><w:i /></w:rPr><w:t xml:space=\"preserve\">italic</w:t></w:r></w:p>");

    [Test]
    public Task ConsecutiveBreaks() =>
        Verify(WordHtmlConverter.ToParagraphs("one<br><br><br>two"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">one</w:t></w:r><w:r><w:br /></w:r><w:r><w:br /></w:r><w:r><w:br /></w:r><w:r><w:t xml:space=\"preserve\">two</w:t></w:r></w:p>");

    [Test]
    public Task LineBreakInElements() =>
        Verify(WordHtmlConverter.ToElements("<p>a<br>b</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">a</w:t></w:r><w:r><w:br /></w:r><w:r><w:t xml:space=\"preserve\">b</w:t></w:r></w:p>");

    [Test]
    public Task LineBreakInListItem() =>
        Verify(WordHtmlConverter.ToElements("<ul><li>a<br>b</li><li>c</li></ul>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">a</w:t></w:r><w:r><w:br /></w:r><w:r><w:t xml:space="preserve">b</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">c</w:t></w:r></w:p>
                """);

    [Test]
    public Task HorizontalRuleStillBreaksParagraph() =>
        Verify(WordHtmlConverter.ToParagraphs("one<hr>two"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">one</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">———</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">two</w:t></w:r></w:p>
                """);


    [Test]
    public Task WhitespaceCollapsing() =>
        Verify(WordHtmlConverter.ToParagraphs("  lots   of    spaces  "))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\"> lots of spaces </w:t></w:r></w:p>");

    [Test]
    public Task WhitespaceFoldsWithinTextNode() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>Line1\r\n\r\nLine2</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Line1 Line2</w:t></w:r></w:p>");

    // Each text node collapses on its own, so without carrying the folding state across the inline
    // boundaries this rendered as "a  x  y   z".
    [Test]
    public Task WhitespaceFoldsAcrossInlineNodes() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>a <b> x</b> <i>y </i> z</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">a </w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">x</w:t></w:r><w:r><w:t xml:space=\"preserve\"> </w:t></w:r><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\">y </w:t></w:r><w:r><w:t xml:space=\"preserve\">z</w:t></w:r></w:p>");

    // Browsers drop the space after a line break. The element path always did; the segment path
    // seeded its fold state from EndsWith(' '), and a <br> segment is "\n", so the two disagreed —
    // ToParagraphs kept the space where ToElements dropped it. Both forms are pinned since the
    // point is that they now agree.
    [Test]
    public Task SpaceAfterBreakIsDroppedInSegments() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>a<br> b</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">a</w:t></w:r><w:r><w:br /></w:r><w:r><w:t xml:space=\"preserve\">b</w:t></w:r></w:p>");

    [Test]
    public Task SpaceAfterBreakIsDroppedInElements() =>
        Verify(WordHtmlConverter.ToElements("<p>a<br> b</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">a</w:t></w:r><w:r><w:br /></w:r><w:r><w:t xml:space=\"preserve\">b</w:t></w:r></w:p>");

    [Test]
    public Task SpecialCharacters() =>
        Verify(WordHtmlConverter.ToParagraphs("price: $100 &amp; tax &lt; 10%"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">price: $100 &amp; tax &lt; 10%</w:t></w:r></w:p>");

    [Test]
    public Task UnknownTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<custom>text</custom>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">text</w:t></w:r></w:p>");

    [Test]
    public Task ImageAlt() =>
        Verify(WordHtmlConverter.ToParagraphs("before <img alt=\"image description\"> after"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before </w:t></w:r><w:r><w:t xml:space=\"preserve\">image description</w:t></w:r><w:r><w:t xml:space=\"preserve\"> after</w:t></w:r></w:p>");

    [Test]
    public Task EmptyTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<b></b><i></i>text"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">text</w:t></w:r></w:p>");

    [Test]
    public Task MalformedHtml() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>overlap</b> still italic</i>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">bold </w:t></w:r><w:r><w:rPr><w:b /><w:i /></w:rPr><w:t xml:space=\"preserve\">overlap</w:t></w:r><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\"> still italic</w:t></w:r></w:p>");

    [Test]
    public Task CiteTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<cite>citation</cite>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\">citation</w:t></w:r></w:p>");

    [Test]
    public Task VarTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<var>variable</var>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\">variable</w:t></w:r></w:p>");

    [Test]
    public Task InvalidXmlCharsFromEntities() =>
        Verify(WordHtmlConverter.ToParagraphs("before&#1;&#0;&#x1F;after"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before�after</w:t></w:r></w:p>");

    [Test]
    public Task InvalidXmlCharsRaw() =>
        Verify(WordHtmlConverter.ToParagraphs("before\u0001\u0000\u001fafter"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">beforeafter</w:t></w:r></w:p>");

    [Test]
    public Task LoneSurrogate() =>
        Verify(WordHtmlConverter.ToParagraphs("before\uD800after"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">beforeafter</w:t></w:r></w:p>");

}
