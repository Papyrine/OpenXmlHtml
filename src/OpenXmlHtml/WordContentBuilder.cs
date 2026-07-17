static partial class WordContentBuilder
{
    static readonly HtmlParser parser = new();

    internal static List<OpenXmlElement> Build(string html, MainDocumentPart? main, HtmlConvertSettings? settings = null)
    {
        var document = parser.ParseDocument(string.Concat("<body>", html, "</body>"));
        var body = document.Body!;
        var elements = new List<OpenXmlElement>();
        var context = new WordBuildContext
        {
            MainPart = main,
            Settings = settings,
            StyleMap = WordStyleLookup.BuildStyleMap(main)
        };
        if (main?.NumberingDefinitionsPart?.Numbering is { } existingNumbering)
        {
            context.NextNumId = WordNumberingBuilder.GetNextId(existingNumbering);
        }
        else
        {
            context.NextNumId = 1;
        }

        // Seed the footnote counter from any existing footnotes so a later Build call against
        // the same document continues numbering instead of emitting colliding footnote IDs.
        if (main?.FootnotesPart?.Footnotes is { } existingFootnotes)
        {
            context.FootnoteIndex = (int)existingFootnotes
                .Elements<Footnote>()
                .Select(_ => _.Id?.Value ?? 0)
                .DefaultIfEmpty(0)
                .Max();
        }

        if (settings?.NumberingSession is { } session)
        {
            context.BulletAbstractNumId = session.BulletAbstractNumId;
        }

        var rootFormat = new FormatState();
        if (document.DocumentElement is { } htmlRoot)
        {
            ApplyDirAttribute(htmlRoot, ref rootFormat);
        }

        ApplyDirAttribute(body, ref rootFormat);
        if (rootFormat.RightToLeft)
        {
            context.ParagraphRightToLeft = true;
        }

        ProcessChildren(body, rootFormat, elements, context, false);
        FlushParagraph(elements, context);
        TrimTrailingEmptyParagraphs(elements);

        if (settings?.NumberingSession is { } session2)
        {
            session2.BulletAbstractNumId = context.BulletAbstractNumId;
        }

        if (elements.Count == 0)
        {
            elements.Add(new Paragraph());
        }

        return elements;
    }

    static void ProcessChildren(INode node, FormatState format, List<OpenXmlElement> elements, WordBuildContext ctx, bool inPre)
    {
        var orderedIndex = 0;
        foreach (var child in node.ChildNodes)
        {
            switch (child)
            {
                case IText textNode:
                {
                    var text = inPre ? textNode.Data : HtmlSegmentParser.CollapseWhitespace(textNode.Data, ctx.LastWasSpace);
                    if (text.Length > 0 &&
                        !(string.IsNullOrWhiteSpace(text) &&
                          HtmlSegmentParser.IsInterBlockWhitespace(textNode)))
                    {
                        AddTextRun(text, format, ctx);
                        ctx.LastWasSpace = !inPre && text[^1] == ' ';
                    }

                    break;
                }
                case IElement element:
                {
                    var listIndex = element.LocalName == "li" ? ++orderedIndex : 0;
                    ProcessElement(element, format, elements, ctx, inPre, listIndex);
                    break;
                }
            }
        }
    }

    static void ProcessElement(IElement element, FormatState format, List<OpenXmlElement> elements, WordBuildContext context, bool inPre, int listIndex = 0)
    {
        var tag = element.LocalName;
        var newFormat = format;
        HtmlSegmentParser.ApplyElementFormatting(element, tag, ref newFormat, out var styleDeclarations);
        if (HtmlSegmentParser.IsHiddenElement(element, tag, styleDeclarations))
        {
            return;
        }

        inPre = HtmlSegmentParser.ApplyWhiteSpace(styleDeclarations, ref newFormat, inPre);

        switch (tag)
        {
            case "br":
                AddBreakRun(format, context);
                return;
            case "wbr":
                AddTextRun("\u200B", format, context);
                return;
            case "hr":
                FlushParagraph(elements, context);
                AddTextRun("\u2014\u2014\u2014", format, context);
                FlushParagraph(elements, context);
                return;
            case "img":
            {
                var imageData = ImageResolver.Resolve(element, context.Settings);
                if (imageData == null)
                {
                    var alt = element.GetAttribute("alt");
                    if (!string.IsNullOrEmpty(alt))
                    {
                        // ReSharper disable once RedundantSuppressNullableWarningExpression
                        AddTextRun(alt!, format, context);
                    }
                }
                else
                {
                    if (context.MainPart != null)
                    {
                        context.ImageIndex++;
                        context.CurrentRuns.Add(WordHtmlConverter.BuildImageRun(context.MainPart, imageData, context.ImageIndex));
                    }
                }

                return;
            }
            case "svg":
            {
                if (context.MainPart != null)
                {
                    var imageData = HtmlSegmentParser.ParseSvgElement(element);
                    context.ImageIndex++;
                    context.CurrentRuns.Add(WordHtmlConverter.BuildImageRun(context.MainPart, imageData, context.ImageIndex));
                }

                return;
            }
            case "col":
                return;
            case "input":
            {
                var inputType = element.GetAttribute("type");
                if (string.Equals(inputType, "checkbox", StringComparison.OrdinalIgnoreCase))
                {
                    var checkedAttr = element.HasAttribute("checked");
                    AddTextRun(checkedAttr ? "☑ " : "☐ ", format, context);
                }

                return;
            }
            case "table":
                FlushParagraph(elements, context);
                BuildTable(element, format, elements, context);
                return;
            case "ul" or "ol":
                BuildList(element, tag, newFormat, elements, context, inPre);
                return;
            case "li":
                BuildListItem(element, newFormat, elements, context, inPre, listIndex);
                return;
            case "a":
                BuildAnchor(element, format, newFormat, elements, context, inPre);
                return;
            case "q":
            {
                AddTextRun("\u201C", format, context);
                ProcessChildren(element, newFormat, elements, context, inPre);
                AddTextRun("\u201D", format, context);
                return;
            }
            case "pre":
            {
                FlushParagraph(elements, context);
                ProcessChildren(element, newFormat, elements, context, true);
                FlushParagraph(elements, context);
                return;
            }
            case "rt":
            {
                AddTextRun("(", format, context);
                var rtFormat = newFormat;
                rtFormat.FontSizePt = Math.Round((rtFormat.FontSizePt ?? 12) * 0.6, 2);
                ProcessChildren(element, rtFormat, elements, context, inPre);
                AddTextRun(")", format, context);
                return;
            }
            case "rp":
                return;
            case "abbr" or "acronym":
            {
                ProcessChildren(element, newFormat, elements, context, inPre);
                var title = element.GetAttribute("title");
                if (!string.IsNullOrEmpty(title) &&
                    context.MainPart != null)
                {
                    // ReSharper disable once RedundantSuppressNullableWarningExpression
                    context.CurrentRuns.Add(BuildFootnoteRun(context, title!));
                }

                return;
            }
        }

        // Handle blockquote with cite attribute as footnote
        if (tag == "blockquote")
        {
            var cite = element.GetAttribute("cite");
            if (!string.IsNullOrEmpty(cite) &&
                context.MainPart != null)
            {
                FlushParagraph(elements, context);
                ProcessChildren(element, newFormat, elements, context, inPre);
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                context.CurrentRuns.Add(BuildFootnoteRun(context, cite!));
                FlushParagraph(elements, context);
                return;
            }
        }

        var isBlock = HtmlSegmentParser.IsBlockElement(tag);
        var pageBreakBefore = false;
        var pageBreakAfter = false;

        if (isBlock)
        {
            ParagraphFormatState? pendingFormat = null;
            if (styleDeclarations != null)
            {
                pageBreakBefore = IsPageBreak(styleDeclarations, "page-break-before") ||
                                  IsPageBreak(styleDeclarations, "break-before");
                pageBreakAfter = IsPageBreak(styleDeclarations, "page-break-after") ||
                                 IsPageBreak(styleDeclarations, "break-after");

                var pf = ParagraphFormatState.ParseFrom(styleDeclarations);
                if (pf.HasProperties)
                {
                    pendingFormat = pf;
                }
            }

            FlushParagraph(elements, context);

            if (pendingFormat != null)
            {
                context.ParagraphFormat = pendingFormat;
            }

            if (newFormat.RightToLeft)
            {
                context.ParagraphRightToLeft = true;
            }

            if (tag is "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
            {
                context.HeadingLevel = tag[1] - '0';
            }

            // CSS class → Word style mapping
            if (context.StyleMap != null &&
                element.ClassList.Length > 0)
            {
                var (paraStyle, runStyle) = WordStyleLookup.LookupClasses(element, context.StyleMap);
                if (paraStyle != null)
                {
                    context.ParagraphStyleId = paraStyle;
                }

                if (runStyle != null)
                {
                    newFormat.RunStyleId = runStyle;
                }
            }

            if (pageBreakBefore)
            {
                elements.Add(
                    new Paragraph(
                        new ParagraphProperties(new PageBreakBefore())));
            }
        }
        else if (context.StyleMap != null && element.ClassList.Length > 0)
        {
            // Inline elements: check for character style
            var (_, runStyle) = WordStyleLookup.LookupClasses(element, context.StyleMap);
            if (runStyle != null)
            {
                newFormat.RunStyleId = runStyle;
            }
        }

        // Add bookmark for elements with id attribute
        var elementId = element.GetAttribute("id") ?? element.GetAttribute("name");
        string? bookmarkId = null;
        if (elementId != null && isBlock)
        {
            context.BookmarkId++;
            bookmarkId = context.BookmarkId.ToString();
            context.CurrentRuns.Add(
                new BookmarkStart
                {
                    Id = bookmarkId,
                    Name = elementId
                });
        }

        ProcessChildren(element, newFormat, elements, context, inPre);

        if (bookmarkId != null)
        {
            context.CurrentRuns.Add(
                new BookmarkEnd
                {
                    Id = bookmarkId
                });
        }

        if (isBlock)
        {
            FlushParagraph(elements, context);

            if (pageBreakAfter)
            {
                elements.Add(
                    new Paragraph(
                        new ParagraphProperties(new PageBreakBefore())));
            }
        }
    }

    static void ApplyDirAttribute(IElement element, ref FormatState format)
    {
        var dir = element.GetAttribute("dir");
        if (dir == null)
        {
            return;
        }

        if (string.Equals(dir, "rtl", StringComparison.OrdinalIgnoreCase))
        {
            format.RightToLeft = true;
        }
        else if (string.Equals(dir, "ltr", StringComparison.OrdinalIgnoreCase))
        {
            format.RightToLeft = false;
        }
    }

    static bool IsPageBreak(Dictionary<string, string> declarations, string key) =>
        declarations.TryGetValue(key, out var value) &&
        (value.Equals("always", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("page", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("left", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("right", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("recto", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("verso", StringComparison.OrdinalIgnoreCase));

    static void AddTextRun(string text, FormatState format, WordBuildContext ctx)
    {
        var run = new Run();

        if (format.HasFormatting)
        {
            run.Append(WordHtmlConverter.BuildWordRunProperties(format));
        }

        run.Append(
            new Text(ApplyTextTransform(text, format))
            {
                Space = SpaceProcessingModeValues.Preserve
            });
        ctx.CurrentRuns.Add(run);
    }

    static void AddBreakRun(FormatState format, WordBuildContext ctx)
    {
        var run = new Run();

        if (format.HasFormatting)
        {
            run.Append(WordHtmlConverter.BuildWordRunProperties(format));
        }

        run.Append(new Break());
        ctx.CurrentRuns.Add(run);

        // A browser drops whitespace straight after a <br>, so treat the break as a space for
        // folding purposes.
        ctx.LastWasSpace = true;
    }

    internal static string ApplyTextTransform(string text, FormatState format)
    {
        var transformed = format.TextTransform switch
        {
            "uppercase" => text.ToUpperInvariant(),
            "lowercase" => text.ToLowerInvariant(),
            "capitalize" => CapitalizeWords(text),
            _ => text
        };

        if (format.NoWrap)
        {
            transformed = transformed.Replace(' ', '\u00A0');
        }

        return XmlCharFilter.StripInvalidXmlChars(transformed);
    }

    static string CapitalizeWords(string text)
    {
        var chars = text.ToCharArray();
        var capitalizeNext = true;
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i]))
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
                capitalizeNext = false;
            }
        }

        return new(chars);
    }

    static void FlushParagraph(List<OpenXmlElement> elements, WordBuildContext context)
    {
        context.LastWasSpace = false;
        if (context.CurrentRuns.Count == 0)
        {
            context.HeadingLevel = 0;
            context.ParagraphStyleId = null;
            context.ParagraphFormat = null;
            context.ParagraphRightToLeft = false;
            if (context.ListItemDepth == 0)
            {
                context.ListNumId = null;
                context.ListIlvl = null;
                context.ListInside = false;
                context.ListDepth = 0;
            }

            return;
        }

        var paragraph = WordHtmlConverter.BuildParagraph(context.CurrentRuns, context.ListNumId != null ? 0 : context.ListDepth);

        // Apply paragraph style: heading > CSS class > default
        if (context.HeadingLevel > 0)
        {
            var offset = context.Settings?.HeadingLevelOffset ?? 0;
            var level = Math.Clamp(context.HeadingLevel + offset, 1, 9);
            paragraph.ParagraphProperties ??= new();
            paragraph.ParagraphProperties.ParagraphStyleId = new()
            {
                Val = $"Heading{level}"
            };
        }
        else if (context.ParagraphStyleId != null)
        {
            paragraph.ParagraphProperties ??= new();
            paragraph.ParagraphProperties.ParagraphStyleId = new()
            {
                Val = context.ParagraphStyleId
            };
        }

        // Apply real Word numbering
        if (context.ListNumId != null)
        {
            paragraph.ParagraphProperties ??= new();
            paragraph.ParagraphProperties.ParagraphStyleId ??= new()
            {
                Val = "ListParagraph"
            };
            paragraph.ParagraphProperties.Append(
                new NumberingProperties(
                    new NumberingLevelReference
                    {
                        Val = context.ListIlvl ?? 0
                    },
                    new NumberingId
                    {
                        Val = context.ListNumId.Value
                    }),
                new ContextualSpacing());

            if (context.ListInside)
            {
                paragraph.ParagraphProperties.Append(
                    new Indentation
                    {
                        Hanging = "0"
                    });
            }
        }

        // Apply paragraph format (CSS margins, alignment, line-height)
        if (context.ParagraphFormat is { HasProperties: true })
        {
            paragraph.ParagraphProperties ??= new();
            ApplyParagraphFormat(paragraph.ParagraphProperties, context.ParagraphFormat);
        }

        if (context.ParagraphRightToLeft &&
            context.ParagraphFormat?.WritingMode == null)
        {
            paragraph.ParagraphProperties ??= new();
            paragraph.ParagraphProperties.Append(new BiDi());
        }

        elements.Add(paragraph);
        context.CurrentRuns.Clear();
        context.ListDepth = 0;
        context.HeadingLevel = 0;
        context.ParagraphStyleId = null;
        context.ParagraphFormat = null;
        context.ParagraphRightToLeft = false;
        context.ListNumId = null;
        context.ListIlvl = null;
        context.ListInside = false;
    }

    static void ApplyParagraphFormat(ParagraphProperties props, ParagraphFormatState pf)
    {
        if (pf.MarginTopTwips != null ||
            pf.MarginBottomTwips != null ||
            pf.LineHeightMultiple != null ||
            pf.LineHeightTwips != null)
        {
            var spacing = new SpacingBetweenLines();
            if (pf.MarginTopTwips != null)
            {
                spacing.Before = pf.MarginTopTwips.Value.ToString();
            }

            if (pf.MarginBottomTwips != null)
            {
                spacing.After = pf.MarginBottomTwips.Value.ToString();
            }

            if (pf.LineHeightMultiple != null)
            {
                spacing.Line = ((int)(pf.LineHeightMultiple.Value * 240)).ToString();
                spacing.LineRule = LineSpacingRuleValues.Auto;
            }
            else if (pf.LineHeightTwips != null)
            {
                spacing.Line = pf.LineHeightTwips.Value.ToString();
                spacing.LineRule = LineSpacingRuleValues.Exact;
            }

            props.Append(spacing);
        }

        if (pf.MarginLeftTwips != null ||
            pf.MarginRightTwips != null ||
            pf.TextIndentTwips != null)
        {
            var existingIndent = props.GetFirstChild<Indentation>();
            var indent = existingIndent ?? new Indentation();
            if (existingIndent == null)
            {
                props.Append(indent);
            }

            if (pf.MarginLeftTwips != null)
            {
                indent.Left = pf.MarginLeftTwips.Value.ToString();
            }

            if (pf.MarginRightTwips != null)
            {
                indent.Right = pf.MarginRightTwips.Value.ToString();
            }

            if (pf.TextIndentTwips != null)
            {
                if (pf.TextIndentTwips.Value >= 0)
                {
                    indent.FirstLine = pf.TextIndentTwips.Value.ToString();
                }
                else
                {
                    indent.Hanging = (-pf.TextIndentTwips.Value).ToString();
                }
            }
        }

        if (pf.TextAlign != null)
        {
            props.Append(
                new Justification
                {
                    Val = pf.TextAlign.Value
                });
        }

        if (pf.BackgroundColor != null)
        {
            props.Append(
                new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Fill = pf.BackgroundColor
                });
        }

        if (pf.WritingMode != null)
        {
            props.Append(new BiDi());
            props.Append(
                new TextDirection
                {
                    Val = pf.WritingMode.Value
                });
        }

        if (pf.BorderTop != null ||
            pf.BorderRight != null ||
            pf.BorderBottom != null ||
            pf.BorderLeft != null)
        {
            var borders = new ParagraphBorders();
            BorderEmitter.AppendSides(borders, pf.BorderTop, pf.BorderLeft, pf.BorderBottom, pf.BorderRight, 1);
            props.Append(borders);
        }
    }

    static void TrimTrailingEmptyParagraphs(List<OpenXmlElement> elements)
    {
        while (elements.Count > 0 &&
               elements[^1] is Paragraph { HasChildren: false })
        {
            elements.RemoveAt(elements.Count - 1);
        }
    }

    static void BuildAnchor(IElement element, FormatState format, FormatState newFormat, List<OpenXmlElement> elements, WordBuildContext context, bool inPre)
    {
        var href = element.GetAttribute("href");

        if (href != null &&
            href.StartsWith('#') &&
            href.Length > 1)
        {
            WrapChildrenInHyperlink(
                element,
                newFormat,
                elements,
                context,
                inPre,
                new()
                {
                    Anchor = href[1..]
                });
            return;
        }

        if (!string.IsNullOrEmpty(href) && context.MainPart != null &&
            Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            var rel = context.MainPart.AddHyperlinkRelationship(uri, true);
            WrapChildrenInHyperlink(
                element,
                newFormat,
                elements,
                context,
                inPre,
                new()
                {
                    Id = rel.Id
                });
            return;
        }

        ProcessChildren(element, newFormat, elements, context, inPre);
        if (!string.IsNullOrEmpty(href))
        {
            if (!element.TextContent.AsSpan().Trim().Equals(href.AsSpan(), StringComparison.Ordinal))
            {
                AddTextRun($" ({href})", format, context);
            }
        }
    }

    static void WrapChildrenInHyperlink(IElement element, FormatState newFormat, List<OpenXmlElement> elements, WordBuildContext context, bool inPre, Hyperlink hyperlink)
    {
        var runsBefore = context.CurrentRuns.Count;
        ProcessChildren(element, newFormat, elements, context, inPre);
        while (context.CurrentRuns.Count > runsBefore)
        {
            var run = context.CurrentRuns[runsBefore];
            context.CurrentRuns.RemoveAt(runsBefore);
            hyperlink.Append(run);
        }

        context.CurrentRuns.Add(hyperlink);
    }
}
