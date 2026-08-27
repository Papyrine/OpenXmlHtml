[TestFixture]
public class SpreadsheetIntegrationTests
{
    [Test]
    public Task SetCellHtml()
    {
        using var stream = new MemoryStream();
        using var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new(new Sheets());
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new(new SheetData());

        var sheets = workbookPart.Workbook.GetFirstChild<Sheets>()!;
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Sheet1"
        });

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
        var row = new Row
        {
            RowIndex = 1
        };
        sheetData.Append(row);

        var cell = new SpreadsheetCell
        {
            CellReference = "A1"
        };
        row.Append(cell);

        SpreadsheetHtmlConverter.SetCellHtml(cell, "<b>Hello</b> <i>World</i>");

        return Verify(cell)
            .Snapshot("<x:c r=\"A1\" t=\"inlineStr\" xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:is><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Hello</x:t></x:r><x:r><x:t xml:space=\"preserve\"> </x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">World</x:t></x:r></x:is></x:c>");
    }

    [Test]
    public Task RichFormattedCell()
    {
        var cell = new SpreadsheetCell();
        SpreadsheetHtmlConverter.SetCellHtml(
            cell,
            """
            <p>Report <b>Summary</b></p>
            <ul>
              <li><span style="color: red">Critical</span>: 3</li>
              <li><span style="color: green">Passed</span>: 47</li>
            </ul>
            """);
        return Verify(cell)
            .Snapshot(
                """
                <x:c t="inlineStr" xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:is><x:r><x:t xml:space="preserve">Report </x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Summary</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:rPr><x:color rgb="FFFF0000" /></x:rPr><x:t xml:space="preserve">Critical</x:t></x:r><x:r><x:t xml:space="preserve">: 3</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:rPr><x:color rgb="FF008000" /></x:rPr><x:t xml:space="preserve">Passed</x:t></x:r><x:r><x:t xml:space="preserve">: 47</x:t></x:r></x:is></x:c>
                """);
    }

    [Test]
    public Task NestedListXlsx()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            var row = new Row
            {
                RowIndex = 1
            };
            var cell = new SpreadsheetCell();
            SpreadsheetHtmlConverter.SetCellHtml(
                cell,
                """
                <ul>
                  <li>Top level</li>
                  <li>
                    <ul>
                      <li>Nested item</li>
                      <li>
                        <ul>
                          <li>Deep item</li>
                        </ul>
                      </li>
                    </ul>
                  </li>
                  <li>Back to top</li>
                </ul>
                """,
                worksheetPart);
            row.Append(cell);
            sheetData.Append(row);
        }

        stream.Position = 0;
        return Verify(stream, "xlsx");
    }

    [Test]
    public Task SingleLinkHyperlink()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            var row = new Row
            {
                RowIndex = 1
            };
            var cell = new SpreadsheetCell
            {
                CellReference = "A1"
            };
            SpreadsheetHtmlConverter.SetCellHtml(
                cell,
                """See the <a href="https://example.com/report">full report</a> for details.""",
                worksheetPart);
            row.Append(cell);
            sheetData.Append(row);
        }

        stream.Position = 0;
        return Verify(stream, "xlsx");
    }

    [Test]
    public Task MailtoLinkHyperlink()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            var row = new Row
            {
                RowIndex = 1
            };
            var cell = new SpreadsheetCell
            {
                CellReference = "A1"
            };
            SpreadsheetHtmlConverter.SetCellHtml(
                cell,
                """Email <a href="mailto:support@example.com">support</a>.""",
                worksheetPart);
            row.Append(cell);
            sheetData.Append(row);
        }

        stream.Position = 0;
        return Verify(stream, "xlsx");
    }

    [Test]
    public Task LinkWithTitleTooltip()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            var row = new Row
            {
                RowIndex = 1
            };
            var cell = new SpreadsheetCell
            {
                CellReference = "A1"
            };
            SpreadsheetHtmlConverter.SetCellHtml(
                cell,
                """<a href="https://example.com" title="Open Example">Example</a>""",
                worksheetPart);
            row.Append(cell);
            sheetData.Append(row);
        }

        stream.Position = 0;
        return Verify(stream, "xlsx");
    }

    [Test]
    public Task MultipleLinkNoHyperlink()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            var row = new Row
            {
                RowIndex = 1
            };
            var cell = new SpreadsheetCell
            {
                CellReference = "A1"
            };
            SpreadsheetHtmlConverter.SetCellHtml(
                cell,
                """<a href="https://example.com">Link 1</a> and <a href="https://other.com">Link 2</a>""",
                worksheetPart);
            row.Append(cell);
            sheetData.Append(row);
        }

        stream.Position = 0;
        return Verify(stream, "xlsx");
    }

    [Test]
    public Task RelativeLinkNoHyperlink()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new(sheetData);

            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            var row = new Row
            {
                RowIndex = 1
            };
            var cell = new SpreadsheetCell
            {
                CellReference = "A1"
            };
            SpreadsheetHtmlConverter.SetCellHtml(
                cell,
                """See <a href="/reports/q1">Q1 report</a>.""",
                worksheetPart);
            row.Append(cell);
            sheetData.Append(row);
        }

        stream.Position = 0;
        return Verify(stream, "xlsx");
    }

    [Test]
    public Task ComplexTable()
    {
        var cell = new SpreadsheetCell();
        SpreadsheetHtmlConverter.SetCellHtml(
            cell,
            """
            <b>Q1 Results</b><br>
            Revenue: <font color="#008000">$1.2M</font><br>
            Expenses: <font color="#FF0000">$800K</font><br>
            <i>Net: <b>$400K</b></i>
            """);
        return Verify(cell)
            .Snapshot(
                """
                <x:c t="inlineStr" xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:is><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Q1 Results</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">Revenue: </x:t></x:r><x:r><x:rPr><x:color rgb="FF008000" /></x:rPr><x:t xml:space="preserve">$1.2M</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">Expenses: </x:t></x:r><x:r><x:rPr><x:color rgb="FFFF0000" /></x:rPr><x:t xml:space="preserve">$800K</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space="preserve">Net: </x:t></x:r><x:r><x:rPr><x:b /><x:i /></x:rPr><x:t xml:space="preserve">$400K</x:t></x:r></x:is></x:c>
                """);
    }
}
