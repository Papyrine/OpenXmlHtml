using WTable = DocumentFormat.OpenXml.Wordprocessing.Table;

[TestFixture]
public class WordTableTests
{
    [Test]
    public Task SimpleTable() =>
        Verify(WordHtmlConverter.ToElements(
            "<table><tr><td>A1</td><td>B1</td></tr><tr><td>A2</td><td>B2</td></tr></table>"));

    [Test]
    public Task TableWithHeaders() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <thead>
                <tr><th>Name</th><th>Value</th></tr>
              </thead>
              <tbody>
                <tr><td>foo</td><td>bar</td></tr>
              </tbody>
            </table>
            """));

    [Test]
    public Task TableWithCaption() =>
        Verify(WordHtmlConverter.ToElements(
            "<table><caption>Table 1</caption><tr><td>data</td></tr></table>"));

    [Test]
    public Task FormattedCellContent() =>
        Verify(WordHtmlConverter.ToElements(
            "<table><tr><td><b>bold</b></td><td><i>italic</i></td></tr></table>"));

    [Test]
    public Task TableWithColspan() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <tr><td colspan="2">Merged</td></tr>
              <tr><td>A</td><td>B</td></tr>
            </table>
            """));

    [Test]
    public Task TableWithRowspan() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <tr><td rowspan="2">Span</td><td>B1</td></tr>
              <tr><td>B2</td></tr>
            </table>
            """));

    [Test]
    public Task NestedTable() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <tr>
                <td>Outer</td>
                <td>
                  <table><tr><td>Inner</td></tr></table>
                </td>
              </tr>
            </table>
            """));

    [Test]
    public Task MixedContentWithTable() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <p>Before table</p>
            <table><tr><td>Cell</td></tr></table>
            <p>After table</p>
            """));

    [Test]
    public Task TableWithTfoot() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <thead><tr><th>Header</th></tr></thead>
              <tbody><tr><td>Body</td></tr></tbody>
              <tfoot><tr><td>Footer</td></tr></tfoot>
            </table>
            """));

    // Every thead row carries tblHeader, so a multi-row header repeats intact. Body rows must not,
    // or Word repeats the whole table.
    [Test]
    public Task TheadRowsRepeatAcrossPages() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <thead><tr><th>H1</th></tr><tr><th>H2</th></tr></thead>
              <tbody><tr><td>Body</td></tr></tbody>
            </table>
            """));

    // A bare table has no thead, so nothing repeats and no trPr is emitted at all.
    [Test]
    public Task TableWithoutTheadHasNoRepeatingRow() =>
        Verify(WordHtmlConverter.ToElements(
            "<table><tr><th>H</th></tr><tr><td>Body</td></tr></table>"));

    // trHeight and tblHeader share a trPr, and CT_TrPrBase requires trHeight first.
    [Test]
    public Task TheadRowWithHeightEmitsBothRowProperties() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <thead><tr style="height: 40px"><th>H</th></tr></thead>
              <tbody><tr><td>Body</td></tr></tbody>
            </table>
            """));

    [Test]
    public Task EmptyTable() =>
        Verify(WordHtmlConverter.ToElements("<table></table>"));

    // Word measures pct widths in fiftieths of a percent, so 35% emits w:w="1750" w:type="pct".
    [Test]
    public Task CellPercentageWidthAttribute() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td width="35%">A</td><td width="65%">B</td></tr></table>"""));

    [Test]
    public Task CellPercentageCssWidth() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td style="width: 35%">A</td></tr></table>"""));

    [Test]
    public Task TablePercentageWidth() =>
        Verify(WordHtmlConverter.ToElements(
            """<table style="width: 100%"><tr><td>A</td></tr></table>"""));

    // A bare number is still px, so this must stay dxa rather than becoming a percentage.
    [Test]
    public Task CellBareNumberWidthStaysDxa() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td width="35">A</td></tr></table>"""));

    [Test]
    public Task CellPixelWidthStaysDxa() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td style="width: 250px">A</td></tr></table>"""));

    // An absolute table width is shared across the columns and switched to fixed layout, because
    // Word's autofit would otherwise treat it as a preference and resize to content. A percentage
    // width deliberately keeps autofit: `w:gridCol` has no percentage unit, so there is nothing to
    // share out and fixed layout is inexpressible — and a percentage-width table with auto columns
    // is what a browser does too. So no tblLayout here, unlike SingleCellTableHonoursExplicitWidth.
    [Test]
    public Task PercentageTableWidthStaysOnAutofit() =>
        Verify(WordHtmlConverter.ToElements(
            """<table style="width: 35%"><tr><td>A</td><td>B</td></tr></table>"""));

    // A tcPr permits one tcW. Both sources emitted unconditionally, so this produced two — and once
    // percentages parsed they could differ in unit too. Css outranks the presentational attribute.
    [Test]
    public Task CellWidthAttributeAndCssEmitsOneWidth() =>
        Verify(WordHtmlConverter.ToElements(
            """<table><tr><td width="35%" style="width: 200px">A</td></tr></table>"""));

    // Under Word's default autofit layout this rendered as a box hugging "single cell" rather than
    // the 602px asked for, because tblW is only a preferred width there.
    [Test]
    public Task SingleCellTableHonoursExplicitWidth()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """<table style="width: 602px"><tr><td>single cell</td></tr></table>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task TableWidthSharedAcrossColumns() =>
        Verify(WordHtmlConverter.ToElements(
            """<table style="width: 600px"><tr><td>A</td><td>B</td><td>C</td></tr></table>"""));

    // No explicit width means nothing to honour, so autofit stays and no tblLayout is emitted.
    [Test]
    public Task TableWithoutWidthStaysAutofit() =>
        Verify(WordHtmlConverter.ToElements("<table><tr><td>A</td><td>B</td></tr></table>"));

    static List<string?> Grid(string html) =>
        WordHtmlConverter
            .ToElements(html)
            .OfType<WTable>()
            .Single()
            .GetFirstChild<TableGrid>()!
            .Elements<GridColumn>()
            .Select(_ => _.Width?.Value)
            .ToList();

    // Word lays the table out from the grid, so cell widths that never reach it change nothing.
    [Test]
    public void CellWidthsFillTheGrid() =>
        Assert.That(
            Grid("""<table><tr><td style="width:536px">a</td><td style="width:80px">b</td></tr></table>"""),
            Is.EqualTo(new[] {"8040", "1200"}));

    // The table width is shared across the columns only when the cells say nothing, so cells that
    // carry widths keep them rather than being flattened to an even split.
    [Test]
    public void CellWidthsBeatAnEvenShareOfTheTableWidth() =>
        Assert.That(
            Grid("""<table style="width:696px"><tr><td style="width:536px">a</td><td style="width:80px">b</td></tr></table>"""),
            Is.EqualTo(new[] {"8040", "1200"}));

    [Test]
    public void ColgroupOutranksCellWidths() =>
        Assert.That(
            Grid("""<table><colgroup><col style="width:100px"><col style="width:200px"></colgroup><tr><td style="width:500px">a</td><td style="width:500px">b</td></tr></table>"""),
            Is.EqualTo(new[] {"1500", "3000"}));

    // A span makes the cell-to-column mapping ambiguous, so the search moves to the next row.
    [Test]
    public void SpannedRowIsNotAWidthSource() =>
        Assert.That(
            Grid("""<table><tr><td colspan="2">head</td></tr><tr><td style="width:300px">a</td><td style="width:100px">b</td></tr></table>"""),
            Is.EqualTo(new[] {"4500", "1500"}));

    // Half a row of widths cannot lay out a table, so a partly sized row is not a source either.
    [Test]
    public void PartlySizedRowIsNotAWidthSource() =>
        Assert.That(
            Grid("""<table><tr><td style="width:300px">a</td><td>b</td></tr></table>"""),
            Is.EqualTo(new string?[] {null, null}));

    // w:gridCol has no percentage unit. The cells keep their own pct widths; the grid stays bare.
    [Test]
    public void PercentageCellWidthsLeaveTheGridBare() =>
        Assert.That(
            Grid("""<table><tr><td style="width:35%">a</td><td style="width:65%">b</td></tr></table>"""),
            Is.EqualTo(new string?[] {null, null}));
}
