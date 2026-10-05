public class WordNestedTests
{
    [Test]
    public Task BoldItalic() =>
        Verify(WordHtmlConverter.ToParagraphs("<b><i>bold italic</i></b>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:b />
                      <w:i />
                    </w:rPr>
                    <w:t xml:space="preserve">bold italic</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task BoldUnderlineItalic() =>
        Verify(WordHtmlConverter.ToParagraphs("<b><u><i>all three</i></u></b>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:b />
                      <w:i />
                      <w:u w:val="single" />
                    </w:rPr>
                    <w:t xml:space="preserve">all three</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task PartialOverlap() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>bold-italic</i> bold</b>"));

    [Test]
    public Task DeeplyNested() =>
        Verify(WordHtmlConverter.ToParagraphs("<b><i><u><s>all formats</s></u></i></b>"));

    [Test]
    public Task MixedContent() =>
        Verify(WordHtmlConverter.ToParagraphs("start <b>bold <i>both</i></b> <u>under</u> end"));

    [Test]
    public Task NestedColors() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<span style=\"color: red\">outer <span style=\"color: blue\">inner</span> outer</span>"));
}
