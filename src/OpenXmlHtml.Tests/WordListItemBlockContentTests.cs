// A list item holding block content: <li><p>a</p><p>b</p></li> is what a loose markdown list and
// most html editors write. The item's first paragraph carries the numbering. The rest are the same
// item continued: no numbering, and indented to the level's left indent so they sit under its text.
public class WordListItemBlockContentTests
{
    [Test]
    public Task MultiParagraphListItems()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(sampleHtml, stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    internal const string sampleHtml =
        """
        <p>Bulleted list, each item two paragraphs:</p>
        <ul>
          <li><p>First bullet</p><p>Continuation of the first bullet</p></li>
          <li><p>Second bullet</p><p>Continuation of the second bullet</p></li>
        </ul>
        <p>Ordered list, the second item holding a nested list and a table:</p>
        <ol>
          <li><p>Item one</p><p>Continuation of item one</p></li>
          <li>
            <p>Item two</p>
            <ul>
              <li><p>Nested bullet</p><p>Continuation of the nested bullet</p></li>
              <li>Second nested bullet</li>
            </ul>
            <p>Item two again, after the nested list</p>
            <table><tr><td>A table in item two</td></tr></table>
          </li>
          <li><p>Item three</p></li>
        </ol>
        <p>After the lists</p>
        """;

    [Test]
    public async Task LaterParagraphsContinueTheItem() =>
        await Assert.That(Describe("<ul><li><p>first</p><p>second</p></li><li><p>next</p></li></ul>"))
            .IsEquivalentTo(
            [
                "num 0: first",
                "ind 480: second",
                "num 0: next"
            ]);

    [Test]
    public async Task SingleParagraphIsTheItem() =>
        await Assert.That(Describe("<ol><li><p>only</p></li></ol><p>after</p>"))
            .IsEquivalentTo(
            [
                "num 0: only",
                "plain: after"
            ]);

    [Test]
    public async Task ParagraphAfterTextContinuesTheItem() =>
        await Assert.That(Describe("<ol><li>text<p>more</p>tail</li><li>two</li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: text",
                "ind 480: more",
                "ind 480: tail",
                "num 0: two"
            ]);

    [Test]
    public async Task DivsContinueTheItem() =>
        await Assert.That(Describe("<ol><li><div>a</div><div>b</div></li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: a",
                "ind 480: b"
            ]);

    [Test]
    public async Task HeadingPreAndBlockquoteContinueTheItem() =>
        await Assert.That(Describe("<ol><li><p>a</p><h3>head</h3><pre>code</pre><blockquote><p>q1</p><p>q2</p></blockquote></li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: a",
                "ind 480: head",
                "ind 480: code",
                "ind 480: q1",
                "ind 480: q2"
            ]);

    // The nested item's own continuation takes the nested level's indent, and the outer item's
    // paragraph after the nested list goes back to the outer one.
    [Test]
    public async Task NestedListKeepsEachLevelsIndent() =>
        await Assert.That(Describe("<ol><li><p>a</p><ul><li>nested</li><li><p>n1</p><p>n2</p></li></ul><p>tail</p></li><li>two</li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: a",
                "num 1: nested",
                "num 1: n1",
                "ind 960: n2",
                "ind 480: tail",
                "num 0: two"
            ]);

    // An empty nested item wrote no paragraph and left its numbering pending, where the outer
    // item's next paragraph picked it up and became a nested bullet.
    [Test]
    public async Task EmptyNestedItemDoesNotNumberTheOuterContinuation() =>
        await Assert.That(Describe("<ol><li><p>a</p><ul><li></li></ul><p>tail</p></li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: a",
                "ind 480: tail"
            ]);

    [Test]
    public async Task TableInItemIsIndented() =>
        await Assert.That(Describe("<ol><li><p>a</p><table><tr><td>cell</td></tr></table><p>b</p></li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: a",
                "table 480: cell",
                "ind 480: b"
            ]);

    // A table has no paragraph for the numbering to sit on, so the item was never counted and the
    // item after it took its number.
    [Test]
    public async Task ItemOpeningWithATableIsStillCounted() =>
        await Assert.That(Describe("<ol><li><table><tr><td>cell</td></tr></table></li><li>two</li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: ",
                "table 480: cell",
                "num 0: two"
            ]);

    [Test]
    public async Task ItemOpeningWithANestedListIsStillCounted() =>
        await Assert.That(Describe("<ol><li><ul><li>nested</li></ul></li><li>two</li></ol>"))
            .IsEquivalentTo(
            [
                "num 0: ",
                "num 1: nested",
                "num 0: two"
            ]);

    // A left margin inside an item is measured from the item's text.
    [Test]
    public async Task OwnMarginIsAddedToTheItemIndent() =>
        await Assert.That(Describe("""<ol><li><p>a</p><p style="margin-left: 20px; text-align: center">b</p></li></ol>"""))
            .IsEquivalentTo(
            [
                "num 0: a",
                "ind 780: b"
            ]);

    [Test]
    public async Task MarkerlessListContinuesAtItsIndent() =>
        await Assert.That(Describe("""<ul style="list-style-type: none"><li><p>a</p><p>b</p></li></ul>"""))
            .IsEquivalentTo(
            [
                "ind 240: a",
                "ind 240: b"
            ]);

    [Test]
    public async Task InsideListContinuesAtTheLevelIndent() =>
        await Assert.That(Describe("""<ul style="list-style-position: inside"><li><p>a</p><p>b</p></li></ul>"""))
            .IsEquivalentTo(
            [
                "num 0: a",
                "ind 480: b"
            ]);

    // One line for each block of the body: whether it is numbered and at what level, or what it
    // is indented by. The document is saved and read back first, and has to be schema valid.
    static List<string> Describe(string html)
    {
        using var stream = new MemoryStream();
        using (var created = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = created.AddMainDocumentPart();
            main.Document = new(new Body());
            WordHtmlConverter.AppendHtml(main.Document.Body!, html, main);
        }

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var errors = new OpenXmlValidator().Validate(document)
            .Select(_ => $"{_.Description} ({_.Path?.XPath})")
            .ToList();
        if (errors.Count > 0)
        {
            throw new(string.Join("\n", errors));
        }

        var lines = new List<string>();
        foreach (var element in document.MainDocumentPart!.Document!.Body!.ChildElements)
        {
            if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
            {
                var tableIndent = table.GetFirstChild<TableProperties>()?.TableIndentation?.Width?.Value;
                lines.Add($"table {tableIndent}: {table.InnerText}");
                continue;
            }

            if (element is not Paragraph paragraph)
            {
                continue;
            }

            var properties = paragraph.ParagraphProperties;
            var level = properties?.NumberingProperties?.NumberingLevelReference?.Val?.Value;
            var indent = properties?.Indentation;
            if (level != null)
            {
                lines.Add($"num {level}: {paragraph.InnerText}");
            }
            else if (indent?.Left?.Value is { } left)
            {
                if (indent.Hanging != null || indent.FirstLine != null)
                {
                    throw new("A continuation paragraph has a first line indent of its own.");
                }

                lines.Add($"ind {left}: {paragraph.InnerText}");
            }
            else
            {
                lines.Add($"plain: {paragraph.InnerText}");
            }
        }

        return lines;
    }
}
