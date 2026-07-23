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
        var table = new Table();

        var defaultBorder = new BorderInfo(4, BorderValues.Single, "auto");

        var borderAttr = tableElement.GetAttribute("border");
        if (borderAttr != null && int.TryParse(borderAttr, out var borderPx))
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

        var tableBorders = BorderEmitter.BuildTableBorders(defaultBorder);

        var tblPr = new TableProperties(
            new TableWidth
            {
                Width = "0",
                Type = TableWidthUnitValues.Auto
            },
            tableBorders);

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

                var colspan = ParseIntAttribute(cellElement, "colspan", 1);
                var rowspan = ParseIntAttribute(cellElement, "rowspan", 1);

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

        var cellStyle = cellElement.GetAttribute("style");
        if (cellStyle != null)
        {
            var declarations = StyleParser.Parse(cellStyle);
            cellProperties = ApplyCellStyles(declarations, cellProperties);
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
            ReorderCellProperties(cellProperties);
            tc.Append(cellProperties);
        }

        var cellFormat = format;
        HtmlSegmentParser.ApplyElementFormatting(cellElement, cellElement.LocalName, ref cellFormat);

        var cellElements = new List<OpenXmlElement>();
        var cellCtx = new WordBuildContext
        {
            MainPart = parentCtx.MainPart,
            ImageIndex = parentCtx.ImageIndex,
            Settings = parentCtx.Settings,
            StyleMap = parentCtx.StyleMap,
            BulletAbstractNumId = parentCtx.BulletAbstractNumId,
            NextNumId = parentCtx.NextNumId,
            FootnoteIndex = parentCtx.FootnoteIndex,
            BookmarkId = parentCtx.BookmarkId,
            ParagraphRightToLeft = cellFormat.RightToLeft
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

    static TableWidthUnitValues ToWidthUnit(WidthUnit unit) =>
        unit == WidthUnit.Percent ? TableWidthUnitValues.Pct : TableWidthUnitValues.Dxa;

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
            var cellTextDir = cellWritingMode.Equals("vertical-rl", StringComparison.OrdinalIgnoreCase) || cellWritingMode.Equals("tb-rl", StringComparison.OrdinalIgnoreCase)
                ? TextDirectionValues.TopToBottomRightToLeft
                : cellWritingMode.Equals("vertical-lr", StringComparison.OrdinalIgnoreCase) || cellWritingMode.Equals("tb-lr", StringComparison.OrdinalIgnoreCase)
                    ? TextDirectionValues.BottomToTopLeftToRight
                    : (TextDirectionValues?)null;
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
            if (cells.Exists(_ => ParseIntAttribute(_, "colspan", 1) > 1 ||
                                  ParseIntAttribute(_, "rowspan", 1) > 1))
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
        var span = ParseIntAttribute(col, "span", 1);
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

    static List<IElement> GetTableRows(IElement tableElement)
    {
        var rows = new List<IElement>();
        foreach (var child in tableElement.Children)
        {
            switch (child.LocalName)
            {
                case "tr":
                    rows.Add(child);
                    break;
                case "thead" or "tbody" or "tfoot":
                {
                    foreach (var grandchild in child.Children)
                    {
                        if (grandchild.LocalName == "tr")
                        {
                            rows.Add(grandchild);
                        }
                    }

                    break;
                }
            }
        }

        return rows;
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

                    colIndex += spanInfo.Colspan;
                    continue;
                }

                var cell = cells[cellIndex];
                cellIndex++;
                var colspan = ParseIntAttribute(cell, "colspan", 1);
                var rowspan = ParseIntAttribute(cell, "rowspan", 1);
                if (rowspan > 1)
                {
                    rowspanTracker[colIndex] = (rowspan - 1, colspan);
                }

                colIndex += colspan;
            }

            maxCols = Math.Max(maxCols, colIndex);
        }

        return Math.Max(1, maxCols);
    }

    static int ParseIntAttribute(IElement element, string attribute, int defaultValue)
    {
        var value = element.GetAttribute(attribute);
        if (value != null && int.TryParse(value, out var result) && result > 1)
        {
            return result;
        }

        return defaultValue;
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

    // The various cell-style sources append in a convenient order; CT_TcPr requires a fixed
    // schema sequence, so normalise the children before emitting the cell.
    static void ReorderCellProperties(TableCellProperties tcPr)
    {
        var sorted = tcPr.ChildElements
            .OrderBy(_ =>
            {
                var index = Array.IndexOf(cellPropertyOrder, _.GetType());
                return index < 0 ? int.MaxValue : index;
            })
            .ToList();
        tcPr.RemoveAllChildren();
        foreach (var child in sorted)
        {
            tcPr.Append(child);
        }
    }
}
