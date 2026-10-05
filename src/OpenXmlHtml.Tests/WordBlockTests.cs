public class WordBlockTests
{
    [Test]
    public Task Paragraphs() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>first paragraph</p><p>second paragraph</p>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">first paragraph</w:t>
                  </w:r>
                </w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">second paragraph</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Divs() =>
        Verify(WordHtmlConverter.ToParagraphs("<div>first div</div><div>second div</div>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">first div</w:t>
                  </w:r>
                </w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">second div</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Headings() =>
        Verify(WordHtmlConverter.ToParagraphs("<h1>heading one</h1><h2>heading two</h2>"));

    [Test]
    public Task MixedBlocksAndInline() =>
        Verify(WordHtmlConverter.ToParagraphs("<p>text with <b>bold</b></p><p>another <i>paragraph</i></p>"));

    [Test]
    public Task Blockquote() =>
        Verify(WordHtmlConverter.ToParagraphs("<blockquote>quoted text</blockquote>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">quoted text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task PreformattedText() =>
        Verify(WordHtmlConverter.ToParagraphs("<pre>  preserved\n  whitespace</pre>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">  preserved
                  whitespace</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task HorizontalRule() =>
        Verify(WordHtmlConverter.ToParagraphs("above<hr>below"));

    [Test]
    public Task EmptyParagraph() =>
        Verify(WordHtmlConverter.ToElements("<p>first</p><p></p><p>second</p>"));

    [Test]
    public Task EmptyDiv() =>
        Verify(WordHtmlConverter.ToElements("<div>first</div><div></div><div>second</div>"));

    [Test]
    public Task EmptyHeading() =>
        Verify(WordHtmlConverter.ToElements("<h1></h1>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:pPr>
                    <w:pStyle w:val="Heading1" />
                  </w:pPr>
                </w:p>
                """);

    [Test]
    public Task EmptyParagraphKeepsStyle() =>
        Verify(WordHtmlConverter.ToElements("""<p style="text-align: center"></p>"""))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:pPr>
                    <w:jc w:val="center" />
                  </w:pPr>
                </w:p>
                """);

    // A container that happens to be empty means "no content", not "a blank line", so it emits
    // nothing. The single bare paragraph here is the existing "never return an empty list" guarantee.
    [Test]
    public Task EmptyContainersAreNotParagraphs() =>
        Verify(WordHtmlConverter.ToElements("<ul></ul><section></section>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

    // A trailing bare paragraph is still trimmed — an html fragment should not leave a dangling
    // blank line behind it. One carrying paragraph properties is not bare, so it survives.
    [Test]
    public Task TrailingEmptyParagraphIsTrimmed() =>
        Verify(WordHtmlConverter.ToElements("<p>text</p><p></p>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">text</w:t>
                  </w:r>
                </w:p>
                """);

    // The wrapper is not itself empty — only the inner block should produce a paragraph.
    [Test]
    public Task DivWrappingParagraph() =>
        Verify(WordHtmlConverter.ToElements("<div><p>only one paragraph</p></div>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">only one paragraph</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task UnorderedList() =>
        Verify(WordHtmlConverter.ToParagraphs("<ul><li>first</li><li>second</li></ul>"));

    [Test]
    public Task OrderedList() =>
        Verify(WordHtmlConverter.ToParagraphs("<ol><li>first</li><li>second</li><li>third</li></ol>"));

    [Test]
    public Task FormattedListItems() =>
        Verify(WordHtmlConverter.ToParagraphs("<ul><li><b>bold</b> item</li><li><i>italic</i> item</li></ul>"));

    [Test]
    public Task NestedUnorderedLists() =>
        Verify(WordHtmlConverter.ToParagraphs("<ul><li>outer</li><li><ul><li>inner</li></ul></li></ul>"));

    [Test]
    public Task NestedOrderedList() =>
        Verify(WordHtmlConverter.ToParagraphs("<ol><li>first</li><li><ol><li>nested first</li><li>nested second</li></ol></li><li>second</li></ol>"));

    [Test]
    public Task DeeplyNestedList() =>
        Verify(WordHtmlConverter.ToParagraphs("<ul><li>level 0</li><li><ul><li>level 1</li><li><ul><li>level 2</li></ul></li></ul></li></ul>"));

    [Test]
    public Task MixedNestedLists() =>
        Verify(WordHtmlConverter.ToParagraphs("<ul><li>bullet</li><li><ol><li>numbered</li></ol></li></ul>"));

    [Test]
    public Task PageBreakBefore() =>
        Verify(WordHtmlConverter.ToElements(
            """<p>Page one</p><p style="page-break-before: always">Page two</p>"""));

    [Test]
    public Task PageBreakAfter() =>
        Verify(WordHtmlConverter.ToElements(
            """<p style="page-break-after: always">Page one</p><p>Page two</p>"""));

    [Test]
    public Task PageBreakOnDiv() =>
        Verify(WordHtmlConverter.ToElements(
            """<div>First section</div><div style="page-break-before: always">Second section</div>"""));
}
