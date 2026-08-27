[TestFixture]
public class WordColorAndFontTests
{
    [Test]
    public Task FontColorAttribute() =>
        Verify(WordHtmlConverter.ToParagraphs("<font color=\"#FF0000\">red text</font>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:color w:val=\"FF0000\" /></w:rPr><w:t xml:space=\"preserve\">red text</w:t></w:r></w:p>");

    [Test]
    public Task NamedColor() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"color: blue\">blue text</span>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:color w:val=\"0000FF\" /></w:rPr><w:t xml:space=\"preserve\">blue text</w:t></w:r></w:p>");

    [Test]
    public Task RgbColor() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"color: rgb(0, 128, 0)\">green text</span>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:color w:val=\"008000\" /></w:rPr><w:t xml:space=\"preserve\">green text</w:t></w:r></w:p>");

    [Test]
    public Task FontFace() =>
        Verify(WordHtmlConverter.ToParagraphs("<font face=\"Arial\">arial text</font>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" /></w:rPr><w:t xml:space=\"preserve\">arial text</w:t></w:r></w:p>");

    [Test]
    public Task FontSize() =>
        Verify(WordHtmlConverter.ToParagraphs("<font size=\"14\">large text</font>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:sz w:val=\"28\" /></w:rPr><w:t xml:space=\"preserve\">large text</w:t></w:r></w:p>");

    [Test]
    public Task InlineStyleFontFamily() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"font-family: Verdana\">verdana</span>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:rFonts w:ascii=\"Verdana\" w:hAnsi=\"Verdana\" /></w:rPr><w:t xml:space=\"preserve\">verdana</w:t></w:r></w:p>");

    [Test]
    public Task InlineStyleFontSizePt() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"font-size: 24pt\">big text</span>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:sz w:val=\"48\" /></w:rPr><w:t xml:space=\"preserve\">big text</w:t></w:r></w:p>");

    [Test]
    public Task MultipleStyles() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<span style=\"font-weight: bold; font-style: italic; color: #0000FF; font-size: 16pt\">styled</span>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /><w:i /><w:color w:val=\"0000FF\" /><w:sz w:val=\"32\" /></w:rPr><w:t xml:space=\"preserve\">styled</w:t></w:r></w:p>");

    [Test]
    public Task CodeTag() =>
        Verify(WordHtmlConverter.ToParagraphs("Use <code>Console.WriteLine</code> to print"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Use </w:t></w:r><w:r><w:rPr><w:rFonts w:ascii=\"Courier New\" w:hAnsi=\"Courier New\" /></w:rPr><w:t xml:space=\"preserve\">Console.WriteLine</w:t></w:r><w:r><w:t xml:space=\"preserve\"> to print</w:t></w:r></w:p>");

    [Test]
    public Task SmallTag() =>
        Verify(WordHtmlConverter.ToParagraphs("normal <small>smaller</small> normal"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">normal </w:t></w:r><w:r><w:rPr><w:sz w:val=\"19\" /></w:rPr><w:t xml:space=\"preserve\">smaller</w:t></w:r><w:r><w:t xml:space=\"preserve\"> normal</w:t></w:r></w:p>");

    [Test]
    public Task ColorWithBold() =>
        Verify(WordHtmlConverter.ToParagraphs("<b style=\"color: red\">bold red</b>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /><w:color w:val=\"FF0000\" /></w:rPr><w:t xml:space=\"preserve\">bold red</w:t></w:r></w:p>");
}
