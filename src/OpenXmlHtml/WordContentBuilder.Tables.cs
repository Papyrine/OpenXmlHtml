static partial class WordContentBuilder
{
    static void BuildTable(IElement tableElement, FormatState format, List<OpenXmlElement> elements, WordBuildContext ctx)
    {
        var tableFormat = format;
        ApplyDirAttribute(tableElement, ref tableFormat);

        IElement? caption = null;
        foreach (var child in tableElement.Children)
        {
            if (child.LocalName == "caption")
            {
                caption = child;
                break;
            }
        }

        if (caption != null)
        {
            var captionFormat = tableFormat;
            HtmlSegmentParser.ApplyElementFormatting(caption, "caption", ref captionFormat);
            ProcessChildren(caption, captionFormat, elements, ctx, false);
            FlushParagraph(elements, ctx);
        }

        var rows = GetTableRows(tableElement);
        if (rows.Count == 0)
        {
            return;
        }

        var columnCount = GetColumnCount(rows);
        if (columnCount > maxColumns)
        {
            Diagnostic.UnsupportedElement(ctx.Settings, "table", tooManyColumns);
            columnCount = maxColumns;
        }

        var table = new Table();

        var defaultBorder = new BorderInfo(4, BorderValues.Single, "auto");

        var borderAttr = tableElement.GetAttribute("border");
        if (borderAttr != null &&
            int.TryParse(borderAttr, out var borderPx))
        {
            if (borderPx == 0)
            {
                defaultBorder = new(0, BorderValues.None, null);
            }
            else
            {
                defaultBorder = new(borderPx * 4, BorderValues.Single, "auto");
            }
        }

        var tableStyle = tableElement.GetAttribute("style");
        Dictionary<string, string>? declarations = null;
        if (tableStyle != null)
        {
            declarations = StyleParser.Parse(tableStyle);

            if (declarations.TryGetValue("border", out var tableBorderCss))
            {
                defaultBorder = StyleParser.ParseBorder(tableBorderCss) ?? defaultBorder;
            }
        }

        // A class naming a table style hands the table's borders, banding and cell margins to that
        // style. Direct formatting beats a table style in Word, so the defaults below are not
        // emitted alongside it — they would silently override the style that was asked for. Without
        // a style they are the only thing making the table legible, so they stay.
        var styleId = ctx.StyleMap == null ? null : WordStyleLookup.LookupTableStyle(tableElement, ctx.StyleMap);

        var tblPr = new TableProperties();
        if (styleId != null)
        {
            // tblStyle leads the tblPr sequence.
            tblPr.Append(
                new TableStyle
                {
                    Val = styleId
                });
        }

        tblPr.Append(
            new TableWidth
            {
                Width = "0",
                Type = TableWidthUnitValues.Auto
            });

        if (styleId == null)
        {
            tblPr.Append(BorderEmitter.BuildTableBorders(defaultBorder));
        }
        else
        {
            // Which of the style's conditional formats apply. The html says whether the table has a
            // header, and says nothing about the rest: a first or last column is not a thing html
            // marks, and column banding has no way to be asked for. Row banding is on, because a
            // style that defines it defines it to be used.
            tblPr.Append(
                new TableLook
                {
                    FirstRow = HasHeaderRow(rows),
                    LastRow = false,
                    FirstColumn = false,
                    LastColumn = false,
                    NoHorizontalBand = false,
                    NoVerticalBand = true
                });
        }

        WidthValue? explicitTableWidth = null;
        if (declarations != null)
        {
            if (declarations.TryGetValue("width", out var tableWidth))
            {
                explicitTableWidth = StyleParser.ParseWidth(tableWidth);
                if (explicitTableWidth is { } width)
                {
                    tblPr.TableWidth = new()
                    {
                        Width = width.Value.ToString(),
                        Type = ToWidthUnit(width.Unit)
                    };
                }
            }

            if (declarations.TryGetValue("background-color", out var tableBg) ||
                declarations.TryGetValue("background", out tableBg))
            {
                var parsed = ColorParser.Parse(tableBg);
                if (parsed != null)
                {
                    tblPr.Append(
                        new Shading
                        {
                            Val = ShadingPatternValues.Clear,
                            Fill = parsed
                        });
                }
            }

            ApplyTableCellPadding(declarations, tblPr);
        }

        var cellPaddingAttr = tableElement.GetAttribute("cellpadding");
        if (cellPaddingAttr != null)
        {
            var twips = StyleParser.ParseLengthToTwips(cellPaddingAttr);
            if (twips != null)
            {
                tblPr.Append(PaddingHelper.BuildMargin<TableCellMarginDefault>(twips, twips, twips, twips));
            }
        }

        if (tableFormat.RightToLeft)
        {
            tblPr.Append(new BiDiVisual());
        }

        var columnWidths = GetColumnWidths(tableElement, rows, columnCount, ctx.Settings);

        // Word's default table layout is autofit, under which tblW is only a *preferred* width and
        // every column gets resized to fit its content — so an explicit table width renders as a
        // box hugging the text rather than the width that was asked for. Sharing the width out
        // across the columns and switching to fixed layout makes Word honour it. Only absolute
        // widths can be shared out like this: gridCol has no percentage unit.
        if (columnCount > 0 &&
            explicitTableWidth is { Unit: WidthUnit.Twips } tableTwips &&
            columnWidths.TrueForAll(_ => _ == null))
        {
            var each = tableTwips.Value / columnCount;
            for (var i = 0; i < columnCount; i++)
            {
                columnWidths[i] = each;
            }
        }

        // Widths are only meaningful under fixed layout; autofit would recompute them from content.
        if (columnWidths.Exists(_ => _ != null))
        {
            tblPr.TableLayout = new()
            {
                Type = TableLayoutValues.Fixed
            };
        }

        NormaliseProperties(tblPr, tablePropertyOrder);
        table.Append(tblPr);

        var grid = new TableGrid();
        for (var i = 0; i < columnCount; i++)
        {
            var gridCol = new GridColumn();
            if (columnWidths[i] is { } w)
            {
                gridCol.Width = w.ToString();
            }

            grid.Append(gridCol);
        }

        table.Append(grid);

        // Track rowspans: starting column index -> (remaining rows, colspan)
        var rowspanTracker = new Dictionary<int, (int Remaining, int Colspan)>();

        foreach (var row in rows)
        {
            var rowFormat = tableFormat;
            ApplyDirAttribute(row, ref rowFormat);
            var tableRow = new TableRow();

            var rowHeight = row.GetAttribute("style") is { } rowStyle
                ? StyleParser.ParseLengthToTwips(StyleParser.Parse(rowStyle).GetValueOrDefault("height") ?? "")
                : null;
            rowHeight ??= row.GetAttribute("height") is { } rh ? StyleParser.ParseLengthToTwips(rh) : null;

            // A row cannot be less than no height, and the cast below would wrap a negative one.
            if (rowHeight is < 0)
            {
                rowHeight = null;
            }
            else if (rowHeight is > StyleParser.MaxTwips)
            {
                rowHeight = StyleParser.MaxTwips;
            }

            // A thead row repeats at the top of every page the table spans. Without it a table
            // broken across a page break loses its header on each page after the first, which for a
            // long table is the difference between readable and not. Rows are flattened out of
            // thead/tbody/tfoot before rendering, so the section is read back off the parent rather
            // than threaded through. Word repeats only rows that lead the table and ignores
            // tblHeader elsewhere, so a thead written after a tbody needs no handling here.
            var isHeaderRow = row.ParentElement?.LocalName == "thead";
            if (rowHeight != null ||
                isHeaderRow)
            {
                // CT_TrPrBase orders trHeight before tblHeader.
                var rowProperties = new TableRowProperties();
                if (rowHeight != null)
                {
                    rowProperties.Append(
                        new TableRowHeight
                        {
                            Val = (uint)rowHeight.Value,
                            HeightType = HeightRuleValues.AtLeast
                        });
                }

                if (isHeaderRow)
                {
                    rowProperties.Append(new TableHeader());
                }

                tableRow.Append(rowProperties);
            }

            var cells = GetCells(row);
            var cellIndex = 0;
            var colIndex = 0;

            while (colIndex < columnCount)
            {
                if (rowspanTracker.TryGetValue(colIndex, out var spanInfo))
                {
                    // CT_TcPr requires gridSpan before vMerge.
                    var contTcPr = new TableCellProperties();
                    if (spanInfo.Colspan > 1)
                    {
                        contTcPr.Append(
                            new GridSpan
                            {
                                Val = spanInfo.Colspan
                            });
                    }

                    contTcPr.Append(new VerticalMerge());
                    tableRow.Append(new TableCell(contTcPr, new Paragraph()));

                    if (spanInfo.Remaining <= 1)
                    {
                        rowspanTracker.Remove(colIndex);
                    }
                    else
                    {
                        rowspanTracker[colIndex] = (spanInfo.Remaining - 1, spanInfo.Colspan);
                    }

                    colIndex += spanInfo.Colspan;
                    continue;
                }

                if (cellIndex >= cells.Count)
                {
                    tableRow.Append(new TableCell(new Paragraph()));
                    colIndex++;
                    continue;
                }

                var cellElement = cells[cellIndex];
                cellIndex++;

                // A span cannot reach past the grid. colspan="100" is a common way to say "all of
                // them", and the columns it names beyond the last one a cell starts in do not exist.
                var colspan = Math.Min(ParseSpan(cellElement, "colspan", maxColspan), columnCount - colIndex);
                var rowspan = ParseSpan(cellElement, "rowspan", maxRowspan);

                int? cellColWidth = null;
                for (var i = 0; i < colspan && colIndex + i < columnWidths.Count; i++)
                {
                    if (columnWidths[colIndex + i] is { } cw)
                    {
                        cellColWidth = (cellColWidth ?? 0) + cw;
                    }
                }

                tableRow.Append(BuildTableCell(cellElement, rowFormat, ctx, colspan, rowspan > 1, cellColWidth));

                if (rowspan > 1)
                {
                    rowspanTracker[colIndex] = (rowspan - 1, colspan);
                }

                colIndex += colspan;
            }

            table.Append(tableRow);
        }

        elements.Add(table);
    }

    static TableCell BuildTableCell(IElement cellElement, FormatState format, WordBuildContext parentCtx, int colspan, bool isRowspanStart, int? colWidth)
    {
        var tc = new TableCell();
        TableCellProperties? cellProperties = null;

        if (colspan > 1 || isRowspanStart)
        {
            cellProperties = new();
            if (colspan > 1)
            {
                cellProperties.Append(
                    new GridSpan
                    {
                        Val = colspan
                    });
            }

            if (isRowspanStart)
            {
                cellProperties.Append(
                    new VerticalMerge
                    {
                        Val = MergedCellValues.Restart
                    });
            }
        }

        // What the cell owes the paragraphs inside it. Word aligns paragraphs, not cells, so a
        // cell's text-align has nowhere to go but onto them. Its margins, border and background
        // are the cell's own and are already in its tcPr.
        ParagraphFormatState? cellParagraphFormat = null;
        var cellStyle = cellElement.GetAttribute("style");
        if (cellStyle != null)
        {
            var declarations = StyleParser.Parse(cellStyle);
            cellProperties = ApplyCellStyles(declarations, cellProperties);

            var parsed = ParagraphFormatState.ParseFrom(declarations);
            if (parsed.TextAlign != null ||
                parsed.LineHeightMultiple != null ||
                parsed.LineHeightTwips != null)
            {
                cellParagraphFormat = new()
                {
                    TextAlign = parsed.TextAlign,
                    LineHeightMultiple = parsed.LineHeightMultiple,
                    LineHeightTwips = parsed.LineHeightTwips
                };
            }
        }

        var bgColorAttr = cellElement.GetAttribute("bgcolor");
        if (bgColorAttr != null)
        {
            var parsed = ColorParser.Parse(bgColorAttr);
            if (parsed != null)
            {
                cellProperties ??= new();
                cellProperties.Append(
                    new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Fill = parsed
                    });
            }
        }

        // ApplyCellStyles above may already have emitted a tcW from a css `width`. A tcPr permits
        // one, and a css declaration outranks a presentational attribute, so the attribute only
        // fills a gap — the same shape as the colgroup fallback below. Without the guard
        // `<td width="35%" style="width:200px">` emitted two, and since percentages parse they
        // could disagree on unit as well as value.
        var widthAttr = cellElement.GetAttribute("width");
        if (widthAttr != null &&
            cellProperties?.GetFirstChild<TableCellWidth>() == null)
        {
            var width = StyleParser.ParseWidth(widthAttr);
            if (width != null)
            {
                cellProperties ??= new();
                cellProperties.Append(
                    new TableCellWidth
                    {
                        Width = width.Value.Value.ToString(),
                        Type = ToWidthUnit(width.Value.Unit)
                    });
            }
        }

        if (colWidth != null &&
            cellProperties?.GetFirstChild<TableCellWidth>() == null)
        {
            cellProperties ??= new();
            cellProperties.Append(
                new TableCellWidth
                {
                    Width = colWidth.Value.ToString(),
                    Type = TableWidthUnitValues.Dxa
                });
        }

        if (cellProperties != null)
        {
            NormaliseProperties(cellProperties, cellPropertyOrder);
            tc.Append(cellProperties);
        }

        var cellFormat = format;
        HtmlSegmentParser.ApplyElementFormatting(cellElement, cellElement.LocalName, ref cellFormat);
        // The cell's own border is its tcBorders, already written above. It is not a border around
        // the text inside the cell.
        cellFormat.Border = format.Border;

        var cellElements = new List<OpenXmlElement>();
        var cellCtx = new WordBuildContext
        {
            MainPart = parentCtx.MainPart,
            RelationshipPart = parentCtx.RelationshipPart,
            ImageIndex = parentCtx.ImageIndex,
            Settings = parentCtx.Settings,
            StyleMap = parentCtx.StyleMap,
            BulletAbstractNumId = parentCtx.BulletAbstractNumId,
            OrderedAbstractNumIds = parentCtx.OrderedAbstractNumIds,
            NextNumId = parentCtx.NextNumId,
            FootnoteIndex = parentCtx.FootnoteIndex,
            BookmarkId = parentCtx.BookmarkId,
            PendingBookmarks = parentCtx.PendingBookmarks,
            StoredImages = parentCtx.StoredImages,
            ParagraphRightToLeft = cellFormat.RightToLeft,
            ParagraphFormat = cellParagraphFormat,
            AmbientParagraphFormat = cellParagraphFormat
        };
        ProcessChildren(cellElement, cellFormat, cellElements, cellCtx, false);
        FlushParagraph(cellElements, cellCtx);
        parentCtx.ImageIndex = cellCtx.ImageIndex;
        parentCtx.NextNumId = cellCtx.NextNumId;
        parentCtx.BulletAbstractNumId = cellCtx.BulletAbstractNumId;
        parentCtx.FootnoteIndex = cellCtx.FootnoteIndex;
        parentCtx.BookmarkId = cellCtx.BookmarkId;

        if (cellElements.Count == 0)
        {
            tc.Append(new Paragraph());
        }
        else
        {
            foreach (var el in cellElements)
            {
                tc.Append(el);
            }

            // OOXML requires every cell to end with a paragraph
            if (cellElements[^1] is not Paragraph)
            {
                tc.Append(new Paragraph());
            }
        }

        return tc;
    }

    static TableWidthUnitValues ToWidthUnit(WidthUnit unit)
    {
        if (unit == WidthUnit.Percent)
        {
            return TableWidthUnitValues.Pct;
        }

        return TableWidthUnitValues.Dxa;
    }

    static TableCellProperties ApplyCellStyles(Dictionary<string, string> declarations, TableCellProperties? tcPr)
    {
        tcPr ??= new();

        if (declarations.TryGetValue("width", out var cellWidth))
        {
            var width = StyleParser.ParseWidth(cellWidth);
            if (width != null)
            {
                tcPr.Append(
                    new TableCellWidth
                    {
                        Width = width.Value.Value.ToString(),
                        Type = ToWidthUnit(width.Value.Unit)
                    });
            }
        }

        if (declarations.TryGetValue("background-color", out var bgColor) ||
            declarations.TryGetValue("background", out bgColor))
        {
            var parsed = ColorParser.Parse(bgColor);
            if (parsed != null)
            {
                tcPr.Append(
                    new Shading
                    {
                        Val = ShadingPatternValues.Clear,
                        Fill = parsed
                    });
            }
        }

        if (declarations.TryGetValue("vertical-align", out var vAlign))
        {
            var val = vAlign.Equals("top", StringComparison.OrdinalIgnoreCase) ? TableVerticalAlignmentValues.Top
                : vAlign.Equals("middle", StringComparison.OrdinalIgnoreCase) ? TableVerticalAlignmentValues.Center
                : vAlign.Equals("bottom", StringComparison.OrdinalIgnoreCase) ? TableVerticalAlignmentValues.Bottom
                : (TableVerticalAlignmentValues?)null;
            if (val != null)
            {
                tcPr.Append(
                    new TableCellVerticalAlignment
                    {
                        Val = val.Value
                    });
            }
        }

        if (declarations.TryGetValue("writing-mode", out var cellWritingMode))
        {
            var cellTextDir = TextDirection(cellWritingMode);
            if (cellTextDir != null)
            {
                tcPr.Append(
                    new TextDirection
                    {
                        Val = cellTextDir.Value
                    });
            }
        }

        if (PaddingHelper.TryParse(declarations) is { } cellPad)
        {
            tcPr.Append(PaddingHelper.BuildMargin<TableCellMargin>(cellPad.Top, cellPad.Right, cellPad.Bottom, cellPad.Left));
        }

        BorderInfo? cellBorderAll = null;
        if (declarations.TryGetValue("border", out var cellBorderVal))
        {
            cellBorderAll = StyleParser.ParseBorder(cellBorderVal);
        }

        var cbt = declarations.TryGetValue("border-top", out var cbtVal) ? StyleParser.ParseBorder(cbtVal) : cellBorderAll;
        var cbr = declarations.TryGetValue("border-right", out var cbrVal) ? StyleParser.ParseBorder(cbrVal) : cellBorderAll;
        var cbb = declarations.TryGetValue("border-bottom", out var cbbVal) ? StyleParser.ParseBorder(cbbVal) : cellBorderAll;
        var cbl = declarations.TryGetValue("border-left", out var cblVal) ? StyleParser.ParseBorder(cblVal) : cellBorderAll;

        if (cbt != null ||
            cbr != null ||
            cbb != null ||
            cbl != null)
        {
            var cellBorders = new TableCellBorders();
            BorderEmitter.AppendSides(cellBorders, cbt, cbl, cbb, cbr, 0);
            tcPr.Append(cellBorders);
        }

        return tcPr;
    }

    static TextDirectionValues? TextDirection(string mode)
    {
        if (mode.Equals("vertical-rl", StringComparison.OrdinalIgnoreCase) ||
            mode.Equals("tb-rl", StringComparison.OrdinalIgnoreCase))
        {
            return TextDirectionValues.TopToBottomRightToLeft;
        }

        if (mode.Equals("vertical-lr", StringComparison.OrdinalIgnoreCase) ||
            mode.Equals("tb-lr", StringComparison.OrdinalIgnoreCase))
        {
            return TextDirectionValues.BottomToTopLeftToRight;
        }

        return null;
    }

    static void ApplyTableCellPadding(Dictionary<string, string> declarations, TableProperties tblPr)
    {
        if (PaddingHelper.TryParse(declarations) is { } pad)
        {
            tblPr.Append(PaddingHelper.BuildMargin<TableCellMarginDefault>(pad.Top, pad.Right, pad.Bottom, pad.Left));
        }
    }

    static List<int?> GetColumnWidths(IElement tableElement, List<IElement> rows, int columnCount, HtmlConvertSettings? settings)
    {
        var widths = new List<int?>(columnCount);

        foreach (var child in tableElement.Children)
        {
            switch (child.LocalName)
            {
                case "colgroup":
                {
                    var hasColChild = false;
                    foreach (var gc in child.Children)
                    {
                        if (gc.LocalName == "col")
                        {
                            hasColChild = true;
                            AddColWidth(gc, widths, settings);
                        }
                    }

                    if (!hasColChild)
                    {
                        AddColWidth(child, widths, settings);
                    }

                    break;
                }
                case "col":
                    AddColWidth(child, widths, settings);
                    break;
            }
        }

        while (widths.Count < columnCount)
        {
            widths.Add(null);
        }

        FillWidthsFromCells(widths, rows, columnCount);

        return widths;
    }

    // Word lays a table out from the grid, so cell widths on their own change nothing: the columns
    // stay evenly split however wide the cells say they are. A colgroup keeps precedence where it
    // is present, and the rest are taken from the first row that maps one cell to one column —
    // which is the row a reader would take them from.
    static void FillWidthsFromCells(List<int?> widths, List<IElement> rows, int columnCount)
    {
        if (!widths.Contains(null))
        {
            return;
        }

        foreach (var row in rows)
        {
            var cells = GetCells(row);
            if (cells.Count != columnCount)
            {
                continue;
            }

            // A span makes the cell-to-column mapping ambiguous, so such a row is not a source.
            if (cells.Exists(_ => ParseSpan(_, "colspan", maxColspan) > 1 ||
                                  ParseSpan(_, "rowspan", maxRowspan) > 1))
            {
                continue;
            }

            // Widths are read without settings, so nothing is reported: a percentage is honoured on
            // the cell itself, and calling that a dropped property here would be wrong. It only
            // means this row cannot fill the grid, so the search moves on.
            var resolved = new List<int?>(columnCount);
            foreach (var cell in cells)
            {
                resolved.Add(ParseColWidth(cell, null));
            }

            if (resolved.Contains(null))
            {
                continue;
            }

            for (var index = 0; index < columnCount; index++)
            {
                widths[index] ??= resolved[index];
            }

            return;
        }
    }

    static void AddColWidth(IElement col, List<int?> widths, HtmlConvertSettings? settings)
    {
        var span = ParseSpan(col, "span", maxColspan);
        var width = ParseColWidth(col, settings);
        for (var i = 0; i < span; i++)
        {
            widths.Add(width);
        }
    }

    // Covers a percentage as well as anything that is not a css length. gridCol has no percentage
    // unit — unlike tcW and tblW, which do, and honour one.
    const string gridColIsAbsolute = "w:gridCol takes an absolute width, so this column width could not be resolved to one";

    static int? ParseColWidth(IElement col, HtmlConvertSettings? settings)
    {
        var style = col.GetAttribute("style");
        if (style != null)
        {
            var declarations = StyleParser.Parse(style);
            if (declarations.TryGetValue("width", out var cssWidth))
            {
                var twips = StyleParser.ParseLengthToTwips(cssWidth);
                if (twips != null)
                {
                    return twips;
                }

                Diagnostic.DroppedProperty(settings, "width", cssWidth, gridColIsAbsolute);
            }
        }

        var widthAttr = col.GetAttribute("width");
        if (widthAttr == null)
        {
            return null;
        }

        if (!widthAttr.EndsWith('%') &&
            StyleParser.ParseLengthToTwips(widthAttr) is { } attrTwips)
        {
            return attrTwips;
        }

        Diagnostic.IgnoredAttribute(settings, "width", widthAttr, gridColIsAbsolute);
        return null;
    }

    // Read back off the parent, the same way the per-row tblHeader is: rows are flattened out of
    // thead/tbody/tfoot before rendering.
    static bool HasHeaderRow(List<IElement> rows)
    {
        foreach (var row in rows)
        {
            if (row.ParentElement?.LocalName == "thead")
            {
                return true;
            }
        }

        return false;
    }

    // The rows in the order a table is drawn: head, then body, then foot, whatever order the
    // sections were written in. html 4 required <tfoot> to come before <tbody>, so markup written
    // to it has the footer second in the source, and read in source order it landed between the
    // header and the body.
    static List<IElement> GetTableRows(IElement tableElement)
    {
        var rows = new List<IElement>();
        List<IElement>? body = null;
        List<IElement>? foot = null;
        foreach (var child in tableElement.Children)
        {
            switch (child.LocalName)
            {
                case "tr":
                    (body ??= []).Add(child);
                    break;
                case "thead":
                    AddSectionRows(child, rows);
                    break;
                case "tbody":
                    AddSectionRows(child, body ??= []);
                    break;
                case "tfoot":
                    AddSectionRows(child, foot ??= []);
                    break;
            }
        }

        if (body != null)
        {
            rows.AddRange(body);
        }

        if (foot != null)
        {
            rows.AddRange(foot);
        }

        return rows;
    }

    static void AddSectionRows(IElement section, List<IElement> rows)
    {
        foreach (var child in section.Children)
        {
            if (child.LocalName == "tr")
            {
                rows.Add(child);
            }
        }
    }

    static List<IElement> GetCells(IElement row)
    {
        var cells = new List<IElement>();
        foreach (var child in row.Children)
        {
            if (child.LocalName is "td" or "th")
            {
                cells.Add(child);
            }
        }

        return cells;
    }

    static int GetColumnCount(List<IElement> rows)
    {
        var maxCols = 0;
        // Mirror the render loop's rowspan tracking so rowspans originating in earlier rows
        // are counted; otherwise later, wider rows would be truncated and cells lost.
        var rowspanTracker = new Dictionary<int, (int Remaining, int Colspan)>();
        foreach (var row in rows)
        {
            var cells = GetCells(row);
            var cellIndex = 0;
            var colIndex = 0;
            while (cellIndex < cells.Count || rowspanTracker.ContainsKey(colIndex))
            {
                if (rowspanTracker.TryGetValue(colIndex, out var spanInfo))
                {
                    if (spanInfo.Remaining <= 1)
                    {
                        rowspanTracker.Remove(colIndex);
                    }
                    else
                    {
                        rowspanTracker[colIndex] = (spanInfo.Remaining - 1, spanInfo.Colspan);
                    }

                    maxCols = Math.Max(maxCols, colIndex + 1);
                    colIndex += spanInfo.Colspan;
                    continue;
                }

                var cell = cells[cellIndex];
                cellIndex++;
                var colspan = ParseSpan(cell, "colspan", maxColspan);
                var rowspan = ParseSpan(cell, "rowspan", maxRowspan);
                if (rowspan > 1)
                {
                    rowspanTracker[colIndex] = (rowspan - 1, colspan);
                }

                // A column exists where a cell starts, not everywhere a span reaches: a browser
                // gives the columns only a span covers no width, so they are not columns here.
                maxCols = Math.Max(maxCols, colIndex + 1);
                colIndex += colspan;
            }

            // A row with fewer cells than the grid stops short of the columns to its right, and a
            // rowspan running down one of those still passes through this row. The render loop
            // visits every column and counts it off there; this one only walks as far as the row's
            // own cells, so the span was left a row too long and pushed a cell in a later row out
            // into a column that does not exist.
            foreach (var column in rowspanTracker.Keys.Where(_ => _ >= colIndex).ToList())
            {
                var (remaining, span) = rowspanTracker[column];
                if (remaining <= 1)
                {
                    rowspanTracker.Remove(column);
                }
                else
                {
                    rowspanTracker[column] = (remaining - 1, span);
                }
            }
        }

        return Math.Max(1, maxCols);
    }

    // The limits html itself puts on colspan, col span and rowspan. Without them a single attribute
    // sized the grid: colspan="1000000" allocated hundreds of megabytes from ninety characters.
    const int maxColspan = 1000;
    const int maxRowspan = 65534;

    // The most columns a Word table can hold.
    const int maxColumns = 63;
    const string tooManyColumns = "a Word table holds at most 63 columns, so the cells past that were dropped";

    static int ParseSpan(IElement element, string attribute, int max)
    {
        var value = element.GetAttribute(attribute);
        if (value != null && int.TryParse(value, out var result) && result > 1)
        {
            return Math.Min(result, max);
        }

        return 1;
    }

    static readonly Type[] cellPropertyOrder =
    [
        typeof(TableCellWidth),
        typeof(GridSpan),
        typeof(VerticalMerge),
        typeof(TableCellBorders),
        typeof(Shading),
        typeof(TableCellMargin),
        typeof(TextDirection),
        typeof(TableCellVerticalAlignment)
    ];

    // CT_TblPrBase, for the children this builder writes. bidiVisual sits ahead of the width, which
    // is the one that was wrong for every table under dir="rtl"; shd, tblLayout and tblCellMar all
    // come before tblLook, which a class-styled table writes early.
    static readonly Type[] tablePropertyOrder =
    [
        typeof(TableStyle),
        typeof(BiDiVisual),
        typeof(TableWidth),
        typeof(TableBorders),
        typeof(Shading),
        typeof(TableLayout),
        typeof(TableCellMarginDefault),
        typeof(TableLook)
    ];

    // The various style sources append in a convenient order, and more than one of them can supply
    // the same property: css and the presentational attribute it replaced, a background and a
    // bgcolor, a padding and a cellpadding. CT_TcPr and CT_TblPr each require a fixed sequence and
    // allow one of each child, so the children are put in schema order and a repeat is dropped.
    // css is read first throughout and the sort is stable, so the one kept is the css one, which
    // is the one that outranks an attribute.
    static void NormaliseProperties(OpenXmlCompositeElement properties, Type[] order)
    {
        var sorted = properties.ChildElements
            .OrderBy(_ =>
            {
                var index = Array.IndexOf(order, _.GetType());
                if (index < 0)
                {
                    return int.MaxValue;
                }

                return index;
            })
            .ToList();
        properties.RemoveAllChildren();
        Type? previous = null;
        foreach (var child in sorted)
        {
            var type = child.GetType();
            if (type == previous &&
                Array.IndexOf(order, type) >= 0)
            {
                continue;
            }

            properties.Append(child);
            previous = type;
        }
    }
}
