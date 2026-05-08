[TestFixture]
public class WordDirectionAttributeTests
{
    [Test]
    public Task ParagraphDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<p dir="rtl">Right-to-left paragraph</p>"""));

    [Test]
    public Task DivDirCascadesToParagraph() =>
        Verify(WordHtmlConverter.ToElements(
            """<div dir="rtl"><p>Inherited from div</p></div>"""));

    [Test]
    public Task SpanDirRunOnly() =>
        Verify(WordHtmlConverter.ToElements(
            """<p>before <span dir="rtl">rtl run</span> after</p>"""));

    [Test]
    public Task DirLtrOverridesAncestor() =>
        Verify(WordHtmlConverter.ToElements(
            """<div dir="rtl"><p>rtl<span dir="ltr"> ltr override</span></p></div>"""));

    [Test]
    public Task TableDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<table dir="rtl"><tr><td>cell</td></tr></table>"""));

    [Test]
    public Task TableCellDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td dir="rtl">rtl cell</td><td>ltr cell</td></tr></table>"""));

    [Test]
    public Task TableRowDirCascadesToCells() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr dir="rtl"><td>cell a</td><td>cell b</td></tr></table>"""));

    [Test]
    public Task BodyDir() =>
        Verify(WordHtmlConverter.ToElements(
            """<body dir="rtl"><p>body inherited</p></body>"""));

    [Test]
    public void DirAttributeEmitsBidi()
    {
        var elements = WordHtmlConverter.ToElements("""<p dir="rtl">x</p>""");
        Assert.That(Xml(elements), Does.Contain("<w:bidi"));
    }

    static string Xml(List<OpenXmlElement> elements) =>
        string.Join('\n', elements.Select(e => e.OuterXml));
}
