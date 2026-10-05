public class WordBasicTests
{
    [Test]
    public Task PlainText() =>
        Verify(WordHtmlConverter.ToParagraphs("Hello world"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">Hello world</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Bold() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold text</b>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:b />
                    </w:rPr>
                    <w:t xml:space="preserve">bold text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Strong() =>
        Verify(WordHtmlConverter.ToParagraphs("<strong>strong text</strong>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:b />
                    </w:rPr>
                    <w:t xml:space="preserve">strong text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Italic() =>
        Verify(WordHtmlConverter.ToParagraphs("<i>italic text</i>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:i />
                    </w:rPr>
                    <w:t xml:space="preserve">italic text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Em() =>
        Verify(WordHtmlConverter.ToParagraphs("<em>emphasized</em>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:i />
                    </w:rPr>
                    <w:t xml:space="preserve">emphasized</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Underline() =>
        Verify(WordHtmlConverter.ToParagraphs("<u>underlined</u>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:u w:val="single" />
                    </w:rPr>
                    <w:t xml:space="preserve">underlined</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Strikethrough() =>
        Verify(WordHtmlConverter.ToParagraphs("<s>struck</s>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:strike />
                    </w:rPr>
                    <w:t xml:space="preserve">struck</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Del() =>
        Verify(WordHtmlConverter.ToParagraphs("<del>deleted</del>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:strike />
                    </w:rPr>
                    <w:t xml:space="preserve">deleted</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task Superscript() =>
        Verify(WordHtmlConverter.ToParagraphs("x<sup>2</sup>"));

    [Test]
    public Task Subscript() =>
        Verify(WordHtmlConverter.ToParagraphs("H<sub>2</sub>O"));

    [Test]
    public Task LineBreak() =>
        Verify(WordHtmlConverter.ToParagraphs("line one<br>line two"));

    [Test]
    public Task WordBreakOpportunity() =>
        Verify(WordHtmlConverter.ToParagraphs("super<wbr>cali<wbr>fragilistic"));

    [Test]
    public Task MixedFormatting() =>
        Verify(WordHtmlConverter.ToParagraphs("normal <b>bold</b> <i>italic</i> normal"));

    [Test]
    public Task EmptyHtml() =>
        Verify(WordHtmlConverter.ToParagraphs(""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

    [Test]
    public Task HtmlEntities() =>
        Verify(WordHtmlConverter.ToParagraphs("&amp; &lt; &gt; &quot;"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">&amp; &lt; &gt; "</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task InsTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<ins>inserted</ins>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:u w:val="single" />
                    </w:rPr>
                    <w:t xml:space="preserve">inserted</w:t>
                  </w:r>
                </w:p>
                """);
}
