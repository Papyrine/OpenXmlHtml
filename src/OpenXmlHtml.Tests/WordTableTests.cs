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
}
