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

        return Verify(cell);
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
        return Verify(cell);
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
        return Verify(cell);
    }

    // hyperlinks was inserted straight after sheetData, ahead of the merged cells and everything
    // else the schema puts between the two. Excel reports a sheet ordered like that as damaged.
    [Test]
    public async Task HyperlinksFollowMergedCellsAndValidations()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var cell = new SpreadsheetCell
            {
                CellReference = "A1"
            };
            worksheetPart.Worksheet = new(
                new SheetData(
                    new Row(cell)
                    {
                        RowIndex = 1
                    }),
                new MergeCells(
                    new MergeCell
                    {
                        Reference = "B1:C1"
                    })
                {
                    Count = 1
                },
                new PageMargins
                {
                    Left = 0.7,
                    Right = 0.7,
                    Top = 0.75,
                    Bottom = 0.75,
                    Header = 0.3,
                    Footer = 0.3
                });
            workbookPart.Workbook = new(
                new Sheets(
                    new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = 1,
                        Name = "Sheet1"
                    }));

            SpreadsheetHtmlConverter.SetCellHtml(cell, """<a href="https://example.com">link</a>""", worksheetPart);
        }

        stream.Position = 0;
        using var reopened = SpreadsheetDocument.Open(stream, false);
        var worksheet = reopened.WorkbookPart!.WorksheetParts.Single().Worksheet!;

        await Assert.That(worksheet.ChildElements.Select(_ => _.LocalName)).IsEquivalentTo(["sheetData", "mergeCells", "hyperlinks", "pageMargins"], CollectionOrdering.Matching);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }
}
