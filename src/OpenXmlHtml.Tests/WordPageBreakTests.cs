using WTable = DocumentFormat.OpenXml.Wordprocessing.Table;

[TestFixture]
public class WordPageBreakTests
{
    [Test]
    public Task PageBreakBefore()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p>Page one</p><p style="page-break-before: always">Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task PageBreakAfter()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p style="page-break-after: always">Page one</p><p>Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task BreakBeforePage()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p>Page one</p><p style="break-before: page">Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task BreakAfterPage()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<p style="break-after: page">Page one</p><p>Page two</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    // The break belongs on the paragraph it breaks before. Spending it on an empty paragraph of its
    // own leaves a blank line at the top of the new page, and renderers collapse the empty paragraph
    // and drop the break with it.
    [Test]
    public void BreakBeforeLandsOnTheBlocksOwnParagraph()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("""<p>Page one</p><p style="page-break-before: always">Page two</p>""")
            .OfType<Paragraph>()
            .ToList();

        Assert.That(paragraphs, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(paragraphs[0].ParagraphProperties?.PageBreakBefore, Is.Null);
            Assert.That(paragraphs[1].ParagraphProperties?.PageBreakBefore, Is.Not.Null);
            Assert.That(paragraphs[1].InnerText, Is.EqualTo("Page two"));
        });
    }

    // Word has no "break after", so it has to become a break before whatever follows.
    [Test]
    public void BreakAfterLandsOnTheFollowingParagraph()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("""<p style="page-break-after: always">Page one</p><p>Page two</p>""")
            .OfType<Paragraph>()
            .ToList();

        Assert.That(paragraphs, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(paragraphs[0].ParagraphProperties?.PageBreakBefore, Is.Null);
            Assert.That(paragraphs[1].ParagraphProperties?.PageBreakBefore, Is.Not.Null);
            Assert.That(paragraphs[1].InnerText, Is.EqualTo("Page two"));
        });
    }

    // With no block of its own to break before, an empty one is the whole point: it is how a break
    // gets written between two things that are not otherwise separated.
    [Test]
    public void BreakOnAnEmptyBlockEmitsASingleBreakParagraph()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("""<p>one</p><div style="page-break-before: always"></div><p>two</p>""")
            .OfType<Paragraph>()
            .ToList();

        Assert.That(paragraphs, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(paragraphs[1].ParagraphProperties?.PageBreakBefore, Is.Not.Null);
            Assert.That(paragraphs[1].InnerText, Is.Empty);
            Assert.That(paragraphs[2].ParagraphProperties?.PageBreakBefore, Is.Null);
        });
    }

    // A table carries no pageBreakBefore, so the break has to fall back to a paragraph ahead of it.
    [Test]
    public void BreakBeforeATableUsesAParagraphAheadOfIt()
    {
        var elements = WordHtmlConverter.ToElements(
            """<div style="page-break-before: always"><table><tr><td>a</td></tr></table></div>""");

        Assert.Multiple(() =>
        {
            Assert.That(elements[0], Is.InstanceOf<Paragraph>());
            Assert.That(((Paragraph) elements[0]).ParagraphProperties?.PageBreakBefore, Is.Not.Null);
            Assert.That(elements[1], Is.InstanceOf<WTable>());
        });
    }
}
