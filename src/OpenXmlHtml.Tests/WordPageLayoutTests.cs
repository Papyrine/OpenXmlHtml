[TestFixture]
public class WordPageLayoutTests
{
    [Test]
    public void AtPageSizeLetter()
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

        Assert.That(pageSize.Width!.Value, Is.EqualTo(12240u));
        Assert.That(pageSize.Height!.Value, Is.EqualTo(15840u));
    }

    [Test]
    public void AtPageSizeA4Landscape()
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

        Assert.That(pageSize.Width!.Value, Is.EqualTo(16838u));
        Assert.That(pageSize.Height!.Value, Is.EqualTo(11906u));
        Assert.That(pageSize.Orient!.Value, Is.EqualTo(PageOrientationValues.Landscape));
    }

    [Test]
    public void AtPageCustomSize()
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

        Assert.That(pageSize.Width!.Value, Is.EqualTo(12240u));
        Assert.That(pageSize.Height!.Value, Is.EqualTo(15840u));
    }

    [Test]
    public void AtPageMargin()
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

        Assert.That(pageMargin.Top!.Value, Is.EqualTo(2880));
        Assert.That(pageMargin.Right!.Value, Is.EqualTo(2880u));
        Assert.That(pageMargin.Bottom!.Value, Is.EqualTo(2880));
        Assert.That(pageMargin.Left!.Value, Is.EqualTo(2880u));
    }

    [Test]
    public void AtPageColumnCount()
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

        Assert.That(columns.ColumnCount!.Value, Is.EqualTo(2));
    }
}
