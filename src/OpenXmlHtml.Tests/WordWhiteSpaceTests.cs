[TestFixture]
public class WordWhiteSpaceTests
{
    [Test]
    public Task WhiteSpacePre() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<div style=\"white-space: pre\">  spaces   preserved\n  and  newlines</div>"));

    [Test]
    public Task WhiteSpacePreWrap() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<div style=\"white-space: pre-wrap\">  multiple   spaces  </div>"));

    [Test]
    public Task WhiteSpaceNowrap() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p style=\"white-space: nowrap\">no breaks here please</p>"));

    [Test]
    public Task WhiteSpaceNormal() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<div style=\"white-space: normal\">   collapsed   spaces   </div>"));

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
    public void PreservedTabBecomesATabElement()
    {
        var elements = WordHtmlConverter.ToElements(
            "<p>cc<span style=\"white-space: pre\">\t</span>Counsel</p>");

        var paragraph = elements.OfType<Paragraph>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(paragraph.Descendants<TabChar>().Count(), Is.EqualTo(1));
            Assert.That(paragraph.InnerText, Is.EqualTo("ccCounsel"));
        });
    }

    [Test]
    public void PreservedTabsSplitTheSurroundingText()
    {
        var elements = WordHtmlConverter.ToElements(
            "<div style=\"white-space: pre\">a\tb\tc</div>");

        var run = elements.OfType<Paragraph>().Single().Descendants<WRun>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(run.Descendants<TabChar>().Count(), Is.EqualTo(2));
            Assert.That(run.Elements<WText>().Select(_ => _.Text), Is.EqualTo(new[] {"a", "b", "c"}));
        });
    }

    // A tab is ordinary whitespace under the default rules, so it folds in with the space around it
    // the way a browser folds it rather than reaching Word as a tab stop.
    [Test]
    public void TabUnderNormalWhiteSpaceFoldsToASpace()
    {
        var elements = WordHtmlConverter.ToElements("<p>cc\tCounsel</p>");

        var paragraph = elements.OfType<Paragraph>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(paragraph.Descendants<TabChar>(), Is.Empty);
            Assert.That(paragraph.InnerText, Is.EqualTo("cc Counsel"));
        });
    }
}
