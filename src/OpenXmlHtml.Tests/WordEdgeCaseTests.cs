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

    const string resolvableImage = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";

    // Where the output cannot hold an image, its alt text stands in for it. That used to happen
    // only when the image failed to resolve: one that resolved and then had nowhere to go left
    // nothing behind.
    [Test]
    public async Task AltTextStandsInWhereThereIsNoDocumentInElements()
    {
        var elements = WordHtmlConverter.ToElements($"""<p>before <img src="{resolvableImage}" alt="Logo"> after</p>""");

        await Assert.That(string.Concat(elements.Select(_ => _.InnerText))).Contains("Logo");
    }

    [Test]
    public async Task AltTextStandsInWhereThereIsNoDocumentInSegments()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs($"""<p>before <img src="{resolvableImage}" alt="Logo"> after</p>""");

        await Assert.That(string.Concat(paragraphs.Select(_ => _.InnerText))).IsEqualTo("before Logo after");
    }

    [Test]
    public async Task AltTextStandsInForAnImageInACell()
    {
        var inline = SpreadsheetHtmlConverter.ToInlineString($"""before <img src="{resolvableImage}" alt="Logo"> after""");

        await Assert.That(inline.InnerText).IsEqualTo("before Logo after");
    }

    [Test]
    public async Task ImageWithoutAltLeavesNothingInACell()
    {
        var inline = SpreadsheetHtmlConverter.ToInlineString($"""before<img src="{resolvableImage}">after""");

        await Assert.That(inline.InnerText).IsEqualTo("beforeafter");
    }

    // A <br> that ends a block is the block's end. The segment path took it for the paragraph
    // break and ran the next block on into the same paragraph; the element path kept it, which Word
    // draws as an empty line a browser does not. Both paths, and compared, so they stay agreed.
    [Test]
    [Arguments("<p>a<br></p><p>b</p>", "a|b")]
    [Arguments("<p>a</p><p><br></p><p>b</p>", "a||b")]
    [Arguments("<div>a<br></div><div>b</div>", "a|b")]
    [Arguments("a<br><p>b</p>", "a|b")]
    [Arguments("<p>a<br><br></p><p>b</p>", "a↵|b")]
    [Arguments("<p>a<br>b</p>", "a↵b")]
    public async Task BreakEndingABlockIsNotALine(string html, string expected)
    {
        await Assert.That(Flatten(WordHtmlConverter.ToElements(html))).IsEqualTo(expected);
        await Assert.That(Flatten(WordHtmlConverter.ToParagraphs(html))).IsEqualTo(expected);
    }

    [Test]
    public async Task BreakEndingABlockIsNotALineInSpreadsheet()
    {
        var inline = SpreadsheetHtmlConverter.ToInlineString("<p>a<br></p><p>b</p>");

        await Assert.That(inline.InnerText).IsEqualTo("a\nb");
    }

    // Paragraphs joined with "|", line breaks shown as "↵".
    static string Flatten(IEnumerable<OpenXmlElement> paragraphs) =>
        string.Join(
            "|",
            paragraphs.Select(paragraph => string.Concat(
                paragraph.Descendants()
                    .Select(_ =>
                    {
                        if (_ is DocumentFormat.OpenXml.Wordprocessing.Break)
                        {
                            return "↵";
                        }

                        if (_ is DocumentFormat.OpenXml.Wordprocessing.Text text)
                        {
                            return text.Text;
                        }

                        return "";
                    }))));
}
