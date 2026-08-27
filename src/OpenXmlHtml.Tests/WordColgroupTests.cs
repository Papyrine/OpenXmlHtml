[TestFixture]
public class WordColgroupTests
{
    [Test]
    public Task ColWidth() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col width="100">
              <col width="200">
              <tr><td>A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"3000\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task ColWidthPx() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col style="width: 100px">
              <col style="width: 200px">
              <tr><td>A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"3000\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task ColWidthInches() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col style="width: 1in">
              <col style="width: 2in">
              <tr><td>A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1440\" /><w:gridCol w:w=\"2880\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1440\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"2880\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task ColSpan() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col span="2" width="100">
              <col width="300">
              <tr><td>A</td><td>B</td><td>C</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"4500\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"4500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">C</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task Colgroup() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <colgroup>
                <col width="100">
                <col width="200">
              </colgroup>
              <tr><td>A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"3000\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task ColgroupWithSpan() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <colgroup span="3" width="150"></colgroup>
              <tr><td>A</td><td>B</td><td>C</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"2250\" /><w:gridCol w:w=\"2250\" /><w:gridCol w:w=\"2250\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"2250\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"2250\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"2250\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">C</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task ColgroupMixedWithLooseCol() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <colgroup>
                <col width="100">
              </colgroup>
              <col width="200">
              <tr><td>A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"3000\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task ColWidthWithColspan() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col width="100">
              <col width="200">
              <col width="300">
              <tr><td colspan="2">Merged</td><td>C</td></tr>
              <tr><td>A</td><td>B</td><td>C</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"3000\" /><w:gridCol w:w=\"4500\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"4500\" w:type=\"dxa\" /><w:gridSpan w:val=\"2\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">Merged</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"4500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">C</w:t></w:r></w:p></w:tc></w:tr><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"4500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">C</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task CellCssWidthOverridesCol() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col width="100">
              <col width="200">
              <tr><td style="width: 500px">A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol w:w=\"3000\" /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"7500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"3000\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    [Test]
    public Task PartialColumnWidths() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col width="100">
              <tr><td>A</td><td>B</td><td>C</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders><w:tblLayout w:type=\"fixed\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1500\" /><w:gridCol /><w:gridCol /></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"1500\" w:type=\"dxa\" /></w:tcPr><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t xml:space=\"preserve\">C</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

    // Narrower than the old name (PercentageWidthIgnored) implied: a percentage on a `td` or on the
    // `table` is honoured and emits w:type="pct". `w:gridCol` has no percentage unit at all, so a
    // percentage there has nowhere to go.
    [Test]
    public Task ColPercentageWidthIgnoredBecauseGridColHasNoPercentUnit() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <table>
              <col width="50%">
              <col width="50%">
              <tr><td>A</td><td>B</td></tr>
            </table>
            """))
            .Snapshot("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders></w:tblPr><w:tblGrid><w:gridCol /><w:gridCol /></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t xml:space=\"preserve\">A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t xml:space=\"preserve\">B</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
}
