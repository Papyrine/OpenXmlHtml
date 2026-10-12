public class WordAnchorTests
{
    [Test]
    public Task SimpleLink() =>
        Verify(WordHtmlConverter.ToParagraphs("<a href=\"https://example.com\">Example</a>"));

    [Test]
    public Task LinkWithSameText() =>
        Verify(WordHtmlConverter.ToParagraphs("<a href=\"https://example.com\">https://example.com</a>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:color w:val="0563C1" />
                      <w:u w:val="single" />
                    </w:rPr>
                    <w:t xml:space="preserve">https://example.com</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task LinkWithFormatting() =>
        Verify(WordHtmlConverter.ToParagraphs("<a href=\"https://example.com\"><b>Bold Link</b></a>"));

    [Test]
    public Task LinkInText() =>
        Verify(WordHtmlConverter.ToParagraphs("Visit <a href=\"https://example.com\">our site</a> for more info."));

    [Test]
    public Task LinkWithNoHref() =>
        Verify(WordHtmlConverter.ToParagraphs("<a>anchor text</a>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">anchor text</w:t>
                  </w:r>
                </w:p>
                """);

    static MainDocumentPart NewMainPart()
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        return document.AddMainDocumentPart();
    }

    // A paragraph can hold a hyperlink per anchor, so ToParagraphs links the text rather than
    // colouring it to look linked and trailing the url in brackets.
    [Test]
    public Task InternalAnchorLinkInParagraphs() =>
        Verify(WordHtmlConverter.ToParagraphs("""<a href="#section2">Jump to Section 2</a>"""));

    // "#name" addresses this document, so it needs no part to register against — which is what
    // lets a bookmark link survive a conversion with no document behind it.
    [Test]
    public Task InternalAnchorLinkNeedsNoMainPart() =>
        Verify(WordHtmlConverter.ToParagraphs("""<a href="#top">Back to top</a>""", null));

    // Asserted rather than snapshotted: the relationship id is generated per part, so a snapshot
    // of it would pin a value that changes every run.
    [Test]
    public async Task ExternalLinkInParagraphs()
    {
        var main = NewMainPart();
        var paragraphs = WordHtmlConverter.ToParagraphs("""<a href="https://example.com">Example</a>""", main);

        var hyperlink = paragraphs.Single()
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Hyperlink>()
            .Single();
        await Assert.That(hyperlink.Anchor).IsNull();
        await Assert.That(hyperlink.InnerText).IsEqualTo("Example");

        var relationship = main.HyperlinkRelationships.Single(_ => _.Id == hyperlink.Id!.Value);
        await Assert.That(relationship.Uri.OriginalString).IsEqualTo("https://example.com");
        await Assert.That(relationship.IsExternal).IsTrue();
    }

    // Formatting inside an anchor must not split it into two links, nor register the relationship
    // twice.
    [Test]
    public Task FormattingInsideAnAnchorStaysOneLink() =>
        Verify(WordHtmlConverter.ToParagraphs("""<a href="#x">plain <b>bold</b> tail</a>""", NewMainPart()));

    // Nothing to link against: a relative href has no base to resolve here, so the text keeps the
    // " (url)" that is the only thing still carrying the target.
    [Test]
    public Task RelativeLinkKeepsItsUrlSuffix() =>
        Verify(WordHtmlConverter.ToParagraphs("""<a href="page.html">Page</a>""", NewMainPart()));

    [Test]
    public Task InternalAnchorLink() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <p><a href="#section2">Jump to Section 2</a></p>
            <h2 id="section2">Section 2</h2>
            <p>Content here.</p>
            """));

    [Test]
    public Task BookmarkOnElement() =>
        Verify(WordHtmlConverter.ToElements(
            """<h1 id="intro">Introduction</h1><p>Some text.</p>"""));

    [Test]
    public Task BookmarksAndLinksDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <h1>Table of Contents</h1>
            <p><a href="#chapter1">Chapter 1: Getting Started</a></p>
            <p><a href="#chapter2">Chapter 2: Advanced Topics</a></p>
            <h1 id="chapter1" style="page-break-before: always">Chapter 1: Getting Started</h1>
            <p>Welcome to the guide.</p>
            <h1 id="chapter2" style="page-break-before: always">Chapter 2: Advanced Topics</h1>
            <p>Deep dive into the subject.</p>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    // An <a> with no href is a named anchor or a placeholder, not a link, and a browser draws it
    // as ordinary text. It was coloured and underlined all the same.
    [Test]
    public Task AnchorWithoutHrefIsNotStyledAsALink() =>
        Verify(WordHtmlConverter.ToElements("""<h2><a name="_Toc1">Introduction</a></h2>"""));

    // text-decoration:none on the element whose tag draws the line removes it. Both paths.
    [Test]
    [Arguments("""<a href="#x" style="text-decoration: none">x</a>""")]
    [Arguments("""<u style="text-decoration: none">x</u>""")]
    [Arguments("""<s style="text-decoration: none">x</s>""")]
    public async Task TextDecorationNoneRemovesTheTagsOwnLine(string html)
    {
        var elements = WordHtmlConverter.ToElements(html).Single();
        var paragraphs = WordHtmlConverter.ToParagraphs(html).Single();

        await Assert.That(elements.Descendants<WUnderline>().Count() + elements.Descendants<WStrike>().Count()).IsEqualTo(0);
        await Assert.That(paragraphs.Descendants<WUnderline>().Count() + paragraphs.Descendants<WStrike>().Count()).IsEqualTo(0);
    }

    // It cannot cancel a line an ancestor drew: css decorations propagate to descendants.
    [Test]
    public async Task TextDecorationNoneDoesNotCancelAnAncestorsLine()
    {
        var paragraph = WordHtmlConverter.ToElements("""<u>a <span style="text-decoration: none">b</span></u>""").Single();

        await Assert.That(paragraph.Descendants<WUnderline>().Count()).IsEqualTo(2);
    }

    // An href that starts with a slash is relative. Uri.TryCreate takes some of them for file
    // paths, and which ones depends on the platform, so they are ruled out before it is asked.
    [Test]
    [Arguments("/docs/page")]
    [Arguments("//example.com/page")]
    [Arguments("\\\\server\\share\\file")]
    [Arguments("page.html")]
    [Arguments("../up")]
    [Arguments("")]
    public async Task RelativeHrefIsNotAnAbsoluteUrl(string href) =>
        await Assert.That(LinkTarget.TryParseAbsolute(href, out _)).IsFalse();

    [Test]
    [Arguments("https://example.com/page")]
    [Arguments("mailto:someone@example.com")]
    [Arguments("ftp://example.com/file")]
    public async Task AbsoluteHrefIsAnAbsoluteUrl(string href) =>
        await Assert.That(LinkTarget.TryParseAbsolute(href, out _)).IsTrue();

    // A protocol-relative link has no scheme to resolve here, so it stays text with its url.
    [Test]
    public async Task ProtocolRelativeLinkIsNotAFileHyperlink()
    {
        var main = NewMainPart();

        var paragraph = WordHtmlConverter.ToElements("""<a href="//example.com/page">page</a>""", main).Single();

        await Assert.That(paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Hyperlink>().Count()).IsEqualTo(0);
        await Assert.That(main.HyperlinkRelationships.Count()).IsEqualTo(0);
        await Assert.That(paragraph.InnerText).IsEqualTo("page (//example.com/page)");
    }

    // A block that holds other blocks has no paragraph of its own. Its bookmark used to be flushed
    // out as one: an empty paragraph holding the start, and another holding the end.
    [Test]
    public Task BookmarkOnContainerAddsNoParagraphs() =>
        Verify(WordHtmlConverter.ToElements(
            """<div id="content"><p>a</p><p>b</p></div><p>c</p>"""));

    [Test]
    public async Task BookmarkOnContainerOfTableLandsInItsFirstCell()
    {
        var elements = WordHtmlConverter.ToElements(
            """<section id="data"><table><tr><td>a</td><td>b</td></tr></table></section>""");

        await Assert.That(elements.Count).IsEqualTo(1);
        var firstCell = elements[0].Descendants<TableCell>().First();
        await Assert.That(firstCell.Descendants<BookmarkStart>().Single().Name!.Value).IsEqualTo("data");
        await Assert.That(elements[0].Descendants<BookmarkEnd>().Count()).IsEqualTo(1);
    }

    // The classic named anchor. It was never bookmarked, so a link to it went nowhere.
    [Test]
    public Task NamedAnchorIsBookmarked() =>
        Verify(WordHtmlConverter.ToElements(
            """<p><a name="top"></a>Top</p><p><a href="#top">Back to top</a></p>"""));

    [Test]
    public Task InlineIdIsBookmarked() =>
        Verify(WordHtmlConverter.ToElements(
            """<p>See <span id="term">the term</span> here.</p>"""));

    // A bookmark id has to be unique in the document. Each call used to count from 1, repeating
    // the ids of the call before and of any bookmark the document already held.
    [Test]
    public async Task BookmarkIdsContinuePastThoseInTheDocument()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body(
            new Paragraph(
                new BookmarkStart
                {
                    Id = "7",
                    Name = "template"
                },
                new BookmarkEnd
                {
                    Id = "7"
                }));
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(body, """<h1 id="one">a</h1><table><tr><td><p id="two">b</p></td></tr></table>""", main);
        WordHtmlConverter.AppendHtml(body, """<h1 id="three">c</h1>""", main);

        var ids = body.Descendants<BookmarkStart>().Select(_ => _.Id!.Value!).ToList();

        await Assert.That(ids).IsEquivalentTo(["7", "8", "9", "10"], CollectionOrdering.Matching);
    }

    // The elements a call returns are not in the document until the caller adds them, so a later
    // call cannot see their bookmarks. A session carries the count across.
    [Test]
    public async Task BookmarkIdsContinueAcrossCallsThatShareASession()
    {
        var main = NewMainPart();
        var settings = new HtmlConvertSettings
        {
            NumberingSession = new()
        };

        var first = WordHtmlConverter.ToElements("""<h1 id="one">a</h1>""", main, settings);
        var second = WordHtmlConverter.ToElements("""<p>no bookmark here</p>""", main, settings);
        var third = WordHtmlConverter.ToElements("""<h1 id="two">b</h1><p><span id="three">c</span></p>""", main, settings);

        var ids = first.Concat(second).Concat(third)
            .SelectMany(_ => _.Descendants<BookmarkStart>())
            .Select(_ => _.Id!.Value!)
            .ToList();

        await Assert.That(ids).IsEquivalentTo(["1", "2", "3"], CollectionOrdering.Matching);
    }
}
