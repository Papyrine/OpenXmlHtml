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
        Verify(WordHtmlConverter.ToParagraphs("  lots   of    spaces  "))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve"> lots of spaces </w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task WhitespaceFoldsWithinTextNode() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>Line1\r\n\r\nLine2</p>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">Line1 Line2</w:t>
                  </w:r>
                </w:p>
                """);

    // Each text node collapses on its own, so without carrying the folding state across the inline
    // boundaries this rendered as "a  x  y   z".
    [Test]
    public Task WhitespaceFoldsAcrossInlineNodes() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>a <b> x</b> <i>y </i> z</p>"));

    // Browsers drop the space after a line break. The element path always did; the segment path
    // seeded its fold state from EndsWith(' '), and a <br> segment is "\n", so the two disagreed —
    // ToParagraphs kept the space where ToElements dropped it. Both forms are pinned since the
    // point is that they now agree.
    [Test]
    public Task SpaceAfterBreakIsDroppedInSegments() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>a<br> b</p>"));

    [Test]
    public Task SpaceAfterBreakIsDroppedInElements() =>
        Verify(WordHtmlConverter.ToElements("<p>a<br> b</p>"));

    [Test]
    public Task SpecialCharacters() =>
        Verify(WordHtmlConverter.ToParagraphs("price: $100 &amp; tax &lt; 10%"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">price: $100 &amp; tax &lt; 10%</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task UnknownTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<custom>text</custom>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task ImageAlt() =>
        Verify(WordHtmlConverter.ToParagraphs("before <img alt=\"image description\"> after"));

    [Test]
    public Task EmptyTags() =>
        Verify(WordHtmlConverter.ToParagraphs("<b></b><i></i>text"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task MalformedHtml() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>overlap</b> still italic</i>"));

    [Test]
    public Task CiteTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<cite>citation</cite>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:i />
                    </w:rPr>
                    <w:t xml:space="preserve">citation</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task VarTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<var>variable</var>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:i />
                    </w:rPr>
                    <w:t xml:space="preserve">variable</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task InvalidXmlCharsFromEntities() =>
        Verify(WordHtmlConverter.ToParagraphs("before&#1;&#0;&#x1F;after"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">before�after</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task InvalidXmlCharsRaw() =>
        Verify(WordHtmlConverter.ToParagraphs("before\u0001\u0000\u001fafter"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">beforeafter</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task LoneSurrogate() =>
        Verify(WordHtmlConverter.ToParagraphs("before\uD800after"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">beforeafter</w:t>
                  </w:r>
                </w:p>
                """);

    // Nesting a few hundred elements deep used to run the thread out of stack, which cannot be
    // caught and takes the process down. Both paths, since each recurses on its own. The text has
    // to survive; the markup past the point the stack runs short does not.
    [Test]
    public async Task DeepNestingKeepsTextInElements()
    {
        var reported = new List<HtmlDiagnostic>();
        var settings = new HtmlConvertSettings
        {
            OnDiagnostic = reported.Add
        };

        var elements = WordHtmlConverter.ToElements(DeeplyNested(), null, settings);

        await Assert.That(string.Concat(elements.Select(_ => _.InnerText))).IsEqualTo("deep");
        await Assert.That(reported.Count).IsEqualTo(1);
        await Assert.That(reported[0].Kind).IsEqualTo(HtmlDiagnosticKind.UnsupportedElement);
    }

    [Test]
    public async Task DeepNestingKeepsTextInSegments()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(DeeplyNested());

        await Assert.That(string.Concat(paragraphs.Select(_ => _.InnerText))).IsEqualTo("deep");
    }

    [Test]
    public async Task DeepNestingKeepsTextInSpreadsheet()
    {
        var inline = SpreadsheetHtmlConverter.ToInlineString(DeeplyNested());

        await Assert.That(inline.InnerText).IsEqualTo("deep");
    }

    static string DeeplyNested()
    {
        const int depth = 20_000;
        var builder = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            builder.Append("<span>");
        }

        builder.Append("deep");
        for (var i = 0; i < depth; i++)
        {
            builder.Append("</span>");
        }

        return builder.ToString();
    }
}
