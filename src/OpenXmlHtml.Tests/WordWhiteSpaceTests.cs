public class WordWhiteSpaceTests
{
    [Test]
    public Task WhiteSpacePre() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<div style=\"white-space: pre\">  spaces   preserved\n  and  newlines</div>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">  spaces   preserved
                  and  newlines</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task WhiteSpacePreWrap() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<div style=\"white-space: pre-wrap\">  multiple   spaces  </div>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">  multiple   spaces  </w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task WhiteSpaceNowrap() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p style=\"white-space: nowrap\">no breaks here please</p>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve">no breaks here please</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task WhiteSpaceNormal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<div style=\"white-space: normal\">   collapsed   spaces   </div>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:t xml:space="preserve"> collapsed spaces </w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task WhiteSpaceConvertToDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            "<p style=\"white-space: pre\">indented  text  preserved</p>",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    // Word ignores a tab inside <w:t>, so keeping one as a character would advance nothing.
    // white-space:pre is what makes a Word tab reachable from html at all.
    [Test]
    public async Task PreservedTabBecomesATabElement()
    {
        var elements = WordHtmlConverter.ToElements(
            "<p>cc<span style=\"white-space: pre\">\t</span>Counsel</p>");

        var paragraph = elements.OfType<Paragraph>().Single();

        using (Assert.Multiple())
        {
            await Assert.That(paragraph.Descendants<TabChar>().Count()).IsEqualTo(1);
            await Assert.That(paragraph.InnerText).IsEqualTo("ccCounsel");
        }
    }

    [Test]
    public async Task PreservedTabsSplitTheSurroundingText()
    {
        var elements = WordHtmlConverter.ToElements(
            "<div style=\"white-space: pre\">a\tb\tc</div>");

        var run = elements.OfType<Paragraph>().Single().Descendants<WRun>().Single();

        using (Assert.Multiple())
        {
            await Assert.That(run.Descendants<TabChar>().Count()).IsEqualTo(2);
            await Assert.That(run.Elements<WText>().Select(_ => _.Text)).IsEquivalentTo(["a", "b", "c"], CollectionOrdering.Matching);
        }
    }

    // A tab is ordinary whitespace under the default rules, so it folds in with the space around it
    // the way a browser folds it rather than reaching Word as a tab stop.
    [Test]
    public async Task TabUnderNormalWhiteSpaceFoldsToASpace()
    {
        var elements = WordHtmlConverter.ToElements("<p>cc\tCounsel</p>");

        var paragraph = elements.OfType<Paragraph>().Single();

        using (Assert.Multiple())
        {
            await Assert.That(paragraph.Descendants<TabChar>()).IsEmpty();
            await Assert.That(paragraph.InnerText).IsEqualTo("cc Counsel");
        }
    }
}
