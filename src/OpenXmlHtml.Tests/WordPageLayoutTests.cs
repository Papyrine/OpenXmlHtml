public class WordPageLayoutTests
{
    [Test]
    public async Task AtPageSizeLetter()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            "<style>@page { size: Letter }</style><p>Body</p>",
            stream);
        stream.Position = 0;

        using var document = WordprocessingDocument.Open(stream, false);
        var pageSize = document.MainDocumentPart!.Document!.Body!
            .GetFirstChild<SectionProperties>()!
            .GetFirstChild<PageSize>()!;

        await Assert.That(pageSize.Width!.Value).IsEqualTo(12240u);
        await Assert.That(pageSize.Height!.Value).IsEqualTo(15840u);
    }

    [Test]
    public async Task AtPageSizeA4Landscape()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            "<style>@page { size: A4 landscape }</style><p>Body</p>",
            stream);
        stream.Position = 0;

        using var document = WordprocessingDocument.Open(stream, false);
        var pageSize = document.MainDocumentPart!.Document!.Body!
            .GetFirstChild<SectionProperties>()!
            .GetFirstChild<PageSize>()!;

        await Assert.That(pageSize.Width!.Value).IsEqualTo(16838u);
        await Assert.That(pageSize.Height!.Value).IsEqualTo(11906u);
        await Assert.That(pageSize.Orient!.Value).IsEqualTo(PageOrientationValues.Landscape);
    }

    [Test]
    public async Task AtPageCustomSize()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            "<style>@page { size: 8.5in 11in }</style><p>Body</p>",
            stream);
        stream.Position = 0;

        using var document = WordprocessingDocument.Open(stream, false);
        var pageSize = document.MainDocumentPart!.Document!.Body!
            .GetFirstChild<SectionProperties>()!
            .GetFirstChild<PageSize>()!;

        await Assert.That(pageSize.Width!.Value).IsEqualTo(12240u);
        await Assert.That(pageSize.Height!.Value).IsEqualTo(15840u);
    }

    [Test]
    public async Task AtPageMargin()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            "<style>@page { margin: 2in }</style><p>Body</p>",
            stream);
        stream.Position = 0;

        using var document = WordprocessingDocument.Open(stream, false);
        var pageMargin = document.MainDocumentPart!.Document!.Body!
            .GetFirstChild<SectionProperties>()!
            .GetFirstChild<PageMargin>()!;

        await Assert.That(pageMargin.Top!.Value).IsEqualTo(2880);
        await Assert.That(pageMargin.Right!.Value).IsEqualTo(2880u);
        await Assert.That(pageMargin.Bottom!.Value).IsEqualTo(2880);
        await Assert.That(pageMargin.Left!.Value).IsEqualTo(2880u);
    }

    [Test]
    public async Task AtPageColumnCount()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            "<style>@page { column-count: 2 }</style><p>Body</p>",
            stream);
        stream.Position = 0;

        using var document = WordprocessingDocument.Open(stream, false);
        var columns = document.MainDocumentPart!.Document!.Body!
            .GetFirstChild<SectionProperties>()!
            .GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Columns>()!;

        await Assert.That(columns.ColumnCount!.Value).IsEqualTo((short) 2);
    }

    // "@page" in the text of the document is not a page rule, and the next brace after it is not
    // its block.
    [Test]
    [Arguments("<p>ping @pagerduty-oncall</p><style>p { margin: 0 }</style>")]
    [Arguments("<p>write @page { margin: 0 } in your stylesheet</p>")]
    [Arguments("<style>@pagefoo { margin: 0 }</style>")]
    [Arguments("<style>@page :first { margin: 3in }</style>")]
    public async Task TextThatIsNotAPageRuleSetsNoLayout(string html) =>
        await Assert.That(AtPageParser.Parse(html)).IsNull();

    // The rule for every page is the one with no selector, wherever it comes.
    [Test]
    [Arguments("<style>@page :first { margin: 3in } @page { margin: 1in }</style>")]
    [Arguments("<style>@page{margin:1in}</style>")]
    [Arguments("<style>p { color: red }</style><STYLE type=\"text/css\">@PAGE { margin: 1in }</STYLE>")]
    [Arguments("<style>@page wide { margin: 3in }\n@page\n{ margin: 1in }</style>")]
    public async Task ThePageRuleWithoutASelectorIsTheOneUsed(string html)
    {
        var layout = AtPageParser.Parse(html)!;

        await Assert.That(layout.MarginTopTwips).IsEqualTo(1440);
    }
}
