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

    // Each nesting level costs a stack frame, and the frame used to hold several copies of the
    // 152-byte format state. Passed by reference it is small enough that markup this deep keeps its
    // formatting on a 4 MB stack, where it used to run out of room and be flattened to text at
    // well under that.
    [Test]
    public async Task DeepButReasonableNestingKeepsItsMarkup()
    {
        const int depth = 1000;
        var builder = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            builder.Append("<span>");
        }

        builder.Append("<b>deep</b>");
        for (var i = 0; i < depth; i++)
        {
            builder.Append("</span>");
        }

        var reported = new List<HtmlDiagnostic>();
        var settings = new HtmlConvertSettings
        {
            OnDiagnostic = reported.Add
        };
        List<OpenXmlElement>? elements = null;
        var thread = new Thread(() => elements = WordHtmlConverter.ToElements(builder.ToString(), null, settings), 4 * 1024 * 1024);
        thread.Start();
        thread.Join();

        await Assert.That(reported).IsEmpty();
        await Assert.That(elements!.Single().Descendants<WBold>().Count()).IsEqualTo(1);
    }

    // Whether whitespace sits between blocks is asked once per parent now rather than once per
    // whitespace node. The answer must not change: dropped between blocks, kept beside inline
    // content, and each parent judged for itself.
    [Test]
    [Arguments("<p>a</p>\n<p>b</p>\n<p>c</p>", "a|b|c")]
    [Arguments("<div>\n<p>a</p>\n<p>b</p>\n</div>", "a|b")]
    [Arguments("<p><b>a</b> <i>b</i> <u>c</u></p>", "a b c")]
    [Arguments("<div>\n<p>a</p>\n</div>\n<p><b>b</b> <i>c</i></p>", "a|b c")]
    public async Task WhitespaceBetweenBlocksIsStillDropped(string html, string expected)
    {
        await Assert.That(string.Join("|", WordHtmlConverter.ToElements(html).Select(_ => _.InnerText))).IsEqualTo(expected);
        await Assert.That(string.Join("|", WordHtmlConverter.ToParagraphs(html).Select(_ => _.InnerText))).IsEqualTo(expected);
    }

    // A long pretty-printed document: many sibling blocks with a newline between each. This took
    // tens of seconds while the check was quadratic.
    [Test]
    public async Task ManyNewlineSeparatedBlocksConvert()
    {
        const int count = 30_000;
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            builder.Append("<p>x</p>\n");
        }

        var html = builder.ToString();

        await Assert.That(WordHtmlConverter.ToElements(html).Count).IsEqualTo(count);
        await Assert.That(WordHtmlConverter.ToParagraphs(html).Count).IsEqualTo(count);
    }
}
