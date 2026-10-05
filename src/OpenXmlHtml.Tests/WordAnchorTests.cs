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
                    <w:rPr>
                      <w:color w:val="0563C1" />
                      <w:u w:val="single" />
                    </w:rPr>
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
}
