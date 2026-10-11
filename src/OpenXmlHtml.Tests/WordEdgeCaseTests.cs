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

    // The element path only moved its fold state for text nodes, so a run written any other way
    // left it stale and the next text node folded against the wrong neighbour. The segment path
    // reads the last segment and always had these right; the two are compared so they stay agreed.
    [Test]
    [Arguments("""see <img src="https://example.com/chart.png" alt="the chart"> below""", "see the chart below")]
    [Arguments("""<p><input type="checkbox" checked> Done</p>""", "☑ Done")]
    [Arguments("<ul>\n<li>\n item</li>\n</ul>", "● item")]
    [Arguments("""a <q> b</q> c""", "a “ b” c")]
    public async Task FoldStateFollowsEveryRun(string html, string expected)
    {
        var elements = string.Concat(WordHtmlConverter.ToElements(html).Select(_ => _.InnerText));
        var segments = string.Concat(WordHtmlConverter.ToParagraphs(html).Select(_ => _.InnerText));

        await Assert.That(elements).IsEqualTo(expected);
        await Assert.That(segments).IsEqualTo(expected);
    }

    // An image is content: the space after it is not a second space to fold into the one before.
    [Test]
    public async Task SpaceAfterImageIsKept()
    {
        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();

        var paragraph = WordHtmlConverter.ToElements($"""<p>a <img src="data:image/png;base64,{png}" width="2" height="2"> b</p>""", main).Single();

        await Assert.That(paragraph.InnerText).IsEqualTo("a  b");
    }
}
