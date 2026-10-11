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
                    <w:t xml:space="preserve">  spaces   preserved</w:t>
                    <w:br />
                    <w:t xml:space="preserve">  and  newlines</w:t>
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

    // Word draws a newline inside <w:t> as a space, so preformatted lines ran together. Both paths,
    // since each writes its own runs.
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task PreNewlinesBecomeLineBreaks(bool elements)
    {
        var paragraph = Convert("<pre>one\ntwo\nthree</pre>", elements).Single();

        await Assert.That(paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>().Count()).IsEqualTo(2);
        await Assert.That(paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(_ => _.Text)).IsEquivalentTo(["one", "two", "three"], CollectionOrdering.Matching);
    }

    // The newline before </pre> closes the last line. It is not a blank line after it, and in the
    // segment path it used to swallow the paragraph break that follows.
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task PreClosingNewlineIsNotALine(bool elements)
    {
        var paragraphs = Convert("<pre>\ncode\n</pre><p>after</p>", elements);

        await Assert.That(paragraphs.Select(_ => _.InnerText)).IsEquivalentTo(["code", "after"], CollectionOrdering.Matching);
        await Assert.That(paragraphs.SelectMany(_ => _.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>()).Count()).IsEqualTo(0);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task WhiteSpacePreNewlinesBecomeLineBreaks(bool elements)
    {
        var paragraph = Convert("<div style=\"white-space: pre\">one\ntwo\n</div>", elements).Single();

        await Assert.That(paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>().Count()).IsEqualTo(1);
    }

    static List<OpenXmlElement> Convert(string html, bool elements)
    {
        if (elements)
        {
            return WordHtmlConverter.ToElements(html);
        }

        return [..WordHtmlConverter.ToParagraphs(html)];
    }
}
