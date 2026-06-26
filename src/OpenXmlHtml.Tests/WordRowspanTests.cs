using WTable = DocumentFormat.OpenXml.Wordprocessing.Table;

[TestFixture]
public class WordRowspanTests
{
    // A rowspan originating in an earlier row must not cause cells in a later, wider row to be
    // dropped. Regression: GetColumnCount ignored carried-over rowspans, so the render loop's
    // column bound was too small and trailing cells were silently lost.
    [Test]
    public void RowspanDoesNotDropTrailingCells()
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

        Assert.That(table.InnerText, Does.Contain("A"));
        Assert.That(table.InnerText, Does.Contain("B"));
        Assert.That(table.InnerText, Does.Contain("C"));

        // Second row is three columns wide: the vMerge continuation of A, then B, then C.
        Assert.That(rows[1].Elements<TableCell>().Count(), Is.EqualTo(3));
    }
}
