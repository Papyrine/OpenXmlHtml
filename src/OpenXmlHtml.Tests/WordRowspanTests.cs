using WTable = DocumentFormat.OpenXml.Wordprocessing.Table;

public class WordRowspanTests
{
    // A rowspan originating in an earlier row must not cause cells in a later, wider row to be
    // dropped. Regression: GetColumnCount ignored carried-over rowspans, so the render loop's
    // column bound was too small and trailing cells were silently lost.
    [Test]
    public async Task RowspanDoesNotDropTrailingCells()
    {
        var elements = WordHtmlConverter.ToElements(
            """
            <table>
              <tr><td rowspan="2">A</td></tr>
              <tr><td>B</td><td>C</td></tr>
            </table>
            """);

        var table = elements.OfType<WTable>().Single();
        var rows = table.Elements<TableRow>().ToList();

        await Assert.That(table.InnerText).Contains("A");
        await Assert.That(table.InnerText).Contains("B");
        await Assert.That(table.InnerText).Contains("C");

        // Second row is three columns wide: the vMerge continuation of A, then B, then C.
        await Assert.That(rows[1].Elements<TableCell>().Count()).IsEqualTo(3);
    }
}
