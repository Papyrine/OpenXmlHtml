[TestFixture]
public class SpreadsheetTableTests
{
    [Test]
    public Task SimpleTable() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<table><tr><td>A1</td><td>B1</td></tr><tr><td>A2</td><td>B2</td></tr></table>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">A1</x:t></x:r><x:r><x:t xml:space="preserve">	</x:t></x:r><x:r><x:t xml:space="preserve">B1</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">A2</x:t></x:r><x:r><x:t xml:space="preserve">	</x:t></x:r><x:r><x:t xml:space="preserve">B2</x:t></x:r></x:is>
                """);

    [Test]
    public Task TableWithHeaders() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            """
            <table>
              <thead>
                <tr><th>Name</th><th>Value</th></tr>
              </thead>
              <tbody>
                <tr><td>foo</td><td>bar</td></tr>
              </tbody>
            </table>
            """))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Name</x:t></x:r><x:r><x:t xml:space="preserve">	</x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Value</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">foo</x:t></x:r><x:r><x:t xml:space="preserve">	</x:t></x:r><x:r><x:t xml:space="preserve">bar</x:t></x:r></x:is>
                """);

    [Test]
    public Task TableWithCaption() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<table><caption>Table 1</caption><tr><td>data</td></tr></table>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Table 1</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">data</x:t></x:r></x:is>
                """);

    [Test]
    public Task TableWithTfoot() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            """
            <table>
              <tbody>
                <tr><td>row</td></tr>
              </tbody>
              <tfoot>
                <tr><td>total</td></tr>
              </tfoot>
            </table>
            """))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">row</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">total</x:t></x:r></x:is>
                """);

    [Test]
    public Task SingleCellTable() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<table><tr><td>only cell</td></tr></table>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">only cell</x:t></x:r></x:is>");

    [Test]
    public Task FormattedCellContent() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<table><tr><td><b>bold</b></td><td><i>italic</i></td></tr></table>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold</x:t></x:r><x:r><x:t xml:space=\"preserve\">\t</x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">italic</x:t></x:r></x:is>");

    [Test]
    public Task ThreeCols() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<table><tr><td>A</td><td>B</td><td>C</td></tr></table>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">A</x:t></x:r><x:r><x:t xml:space=\"preserve\">\t</x:t></x:r><x:r><x:t xml:space=\"preserve\">B</x:t></x:r><x:r><x:t xml:space=\"preserve\">\t</x:t></x:r><x:r><x:t xml:space=\"preserve\">C</x:t></x:r></x:is>");

    [Test]
    public Task ColElement() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<table><col><col><tr><td>A</td><td>B</td></tr></table>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">A</x:t></x:r><x:r><x:t xml:space=\"preserve\">\t</x:t></x:r><x:r><x:t xml:space=\"preserve\">B</x:t></x:r></x:is>");
}
