public class WordTableStyleTests
{
    [Test]
    public Task CellPadding()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td style="padding: 12pt">Padded cell</td>
                <td>Normal cell</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task CellPaddingIndividual()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td style="padding-top: 6pt; padding-bottom: 12pt; padding-left: 24pt; padding-right: 8pt">Custom padding</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task CellWidth()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td style="width: 200pt">Wide cell</td>
                <td style="width: 100pt">Narrow cell</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task CellBackgroundColor()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <th style="background-color: #4472C4; color: white">Header</th>
                <th style="background-color: #4472C4; color: white">Value</th>
              </tr>
              <tr>
                <td>Row 1</td>
                <td style="background-color: #E2EFDA">$100</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task CellVerticalAlign()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td style="vertical-align: top; padding: 12pt">Top</td>
                <td style="vertical-align: middle; padding: 12pt">Middle</td>
                <td style="vertical-align: bottom; padding: 12pt">Bottom</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task TableWidth()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table style="width: 400pt">
              <tr>
                <td>Cell 1</td>
                <td>Cell 2</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task TableBackgroundColor()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table style="background-color: #F2F2F2">
              <tr>
                <td>Shaded table</td>
                <td>Background from table</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task TableDefaultCellPadding()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table style="padding: 8pt">
              <tr>
                <td>Default padding from table</td>
                <td>Applied to all cells</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task HtmlCellpaddingAttribute()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table cellpadding="10">
              <tr>
                <td>Cell with HTML cellpadding</td>
                <td>Applied via attribute</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task HtmlBgcolorAttribute()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td bgcolor="#FFCCCC">Pink via bgcolor attr</td>
                <td>Normal cell</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task HtmlWidthAttribute()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td width="200">Wide via attr</td>
                <td width="100">Narrow via attr</td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task CombinedCellStyles()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table style="width: 500pt">
              <tr>
                <th style="background-color: #2E4057; color: white; padding: 8pt; width: 150pt">Product</th>
                <th style="background-color: #2E4057; color: white; padding: 8pt; width: 100pt">Price</th>
                <th style="background-color: #2E4057; color: white; padding: 8pt">Status</th>
              </tr>
              <tr>
                <td style="padding: 6pt">Widget A</td>
                <td style="padding: 6pt; text-align: right">$29.99</td>
                <td style="padding: 6pt; background-color: #E2EFDA; vertical-align: middle">
                  <span style="color: green"><b>In Stock</b></span>
                </td>
              </tr>
              <tr>
                <td style="padding: 6pt">Widget B</td>
                <td style="padding: 6pt; text-align: right">$49.99</td>
                <td style="padding: 6pt; background-color: #FCE4EC; vertical-align: middle">
                  <span style="color: red"><b>Out of Stock</b></span>
                </td>
              </tr>
            </table>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public async Task CellVerticalAlignPaddedValue()
    {
        var padded = WordHtmlConverter.ToElements(
            """<table><tr><td style="vertical-align:   top   ">x</td></tr></table>""");
        var unpadded = WordHtmlConverter.ToElements(
            """<table><tr><td style="vertical-align: top">x</td></tr></table>""");
        await Assert.That(string.Join('\n', padded.Select(_ => _.OuterXml))).IsEqualTo(string.Join('\n', unpadded.Select(_ => _.OuterXml)));
    }

    // Word aligns paragraphs, not cells, so a cell's text-align goes onto every paragraph in it.
    // It was dropped entirely, which left every right-aligned number column left-aligned.
    [Test]
    public async Task CellTextAlignReachesItsParagraphs()
    {
        var table = WordHtmlConverter.ToElements(
            """
            <table><tr>
              <td style="text-align: right">1</td>
              <td style="text-align: center"><p>a</p><p style="text-align: left">b</p></td>
              <td>plain</td>
            </tr></table>
            """).Single();

        var alignments = table.Descendants<Paragraph>()
            .Select(_ => _.ParagraphProperties?.Justification?.Val?.InnerText ?? "none");

        await Assert.That(alignments).IsEquivalentTo(["right", "center", "left", "none"], CollectionOrdering.Matching);
    }

    // The cell's border and background are its own, not its paragraphs'.
    [Test]
    public async Task CellBorderAndBackgroundStayOnTheCell()
    {
        var table = WordHtmlConverter.ToElements(
            """<table><tr><td style="text-align: right; border: 1px solid red; background-color: #eee; margin: 4px">1</td></tr></table>""").Single();

        var paragraphProperties = table.Descendants<Paragraph>().Single().ParagraphProperties!;

        await Assert.That(paragraphProperties.ChildElements.Count).IsEqualTo(1);
        await Assert.That(paragraphProperties.Justification).IsNotNull();
    }

    // A cell's style is parsed once and shared between its cell properties and its run formatting.
    // Both still have to see every declaration.
    [Test]
    public async Task CellStyleStillReachesCellAndRuns()
    {
        var table = WordHtmlConverter.ToElements(
            """<table><tr><td style="background-color: #eeeeee; color: red; font-weight: bold; padding: 4px">x</td></tr></table>""").Single();

        var cell = table.Descendants<TableCell>().Single();
        var run = cell.Descendants<WRun>().Single();

        await Assert.That(cell.TableCellProperties!.Shading!.Fill!.Value).IsEqualTo("EEEEEE");
        await Assert.That(cell.TableCellProperties.TableCellMargin).IsNotNull();
        await Assert.That(run.RunProperties!.Color!.Val!.Value).IsEqualTo("FF0000");
        await Assert.That(run.RunProperties.Bold).IsNotNull();
    }

    // The grid is filled from the first row whose cells all give a width. The search skips a style
    // that mentions none and gives up on a row at its first cell without one; it must still find
    // the row that qualifies.
    [Test]
    public async Task GridWidthsStillComeFromTheFirstFullRow()
    {
        var table = WordHtmlConverter.ToElements(
            """
            <table>
              <tr><td style="color: red">a</td><td style="width: 100px">b</td></tr>
              <tr><td style="WIDTH: 100px">c</td><td width="200">d</td></tr>
            </table>
            """).Single();

        var widths = table.Descendants<GridColumn>().Select(_ => _.Width!.Value!);

        await Assert.That(widths).IsEquivalentTo(["1500", "3000"], CollectionOrdering.Matching);
    }
}
