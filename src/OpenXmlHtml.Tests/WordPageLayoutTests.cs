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
}
