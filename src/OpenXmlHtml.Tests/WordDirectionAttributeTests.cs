[TestFixture]
public class WordDirectionAttributeTests
{
    [Test]
    public Task ParagraphDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<p dir="rtl">Right-to-left paragraph</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">Right-to-left paragraph</w:t></w:r></w:p>");

    [Test]
    public Task DivDirCascadesToParagraph() =>
        Verify(WordHtmlConverter.ToElements(
            """<div dir="rtl"><p>Inherited from div</p></div>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">Inherited from div</w:t></w:r></w:p>");

    [Test]
    public Task SpanDirRunOnly() =>
        Verify(WordHtmlConverter.ToElements(
            """<p>before <span dir="rtl">rtl run</span> after</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before </w:t></w:r><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">rtl run</w:t></w:r><w:r><w:t xml:space=\"preserve\"> after</w:t></w:r></w:p>");

    [Test]
    public Task DirLtrOverridesAncestor() =>
        Verify(WordHtmlConverter.ToElements(
            """<div dir="rtl"><p>rtl<span dir="ltr"> ltr override</span></p></div>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">rtl</w:t></w:r><w:r><w:t xml:space=\"preserve\"> ltr override</w:t></w:r></w:p>");

    [Test]
    public Task TableDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<table dir="rtl"><tr><td>cell</td></tr></table>"""))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:bidiVisual /></w:tblPr><w:tblGrid><w:gridCol /></w:tblGrid><w:tr><w:tc><w:p><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task TableCellDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td dir="rtl">rtl cell</td><td>ltr cell</td></tr></table>"""))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders></w:tblPr><w:tblGrid><w:gridCol /><w:gridCol /></w:tblGrid><w:tr><w:tc><w:p><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">rtl cell</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t xml:space=\"preserve\">ltr cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task TableRowDirCascadesToCells() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr dir="rtl"><td>cell a</td><td>cell b</td></tr></table>"""))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders></w:tblPr><w:tblGrid><w:gridCol /><w:gridCol /></w:tblGrid><w:tr><w:tc><w:p><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">cell a</w:t></w:r></w:p></w:tc><w:tc><w:p><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">cell b</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task BodyDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<body dir="rtl"><p>body inherited</p></body>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:bidi /></w:pPr><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">body inherited</w:t></w:r></w:p>");

    [Test]
    public void DirAttributeEmitsBidi()
    {
        var elements = WordHtmlConverter.ToElements("""<p dir="rtl">x</p>""");
        Assert.That(Xml(elements), Does.Contain("<w:bidi"));
    }

    static string Xml(List<OpenXmlElement> elements) =>
        string.Join('\n', elements.Select(_ => _.OuterXml));
}
