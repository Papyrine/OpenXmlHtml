static partial class WordContentBuilder
{
    static readonly HtmlParser parser = new();

    // An image lives in its own package part, which only a MainDocumentPart can hold. Converting
    // without one resolves the image and then has nowhere to put it, so it disappears.
    internal const string NoMainPartForImages = "an image part requires a MainDocumentPart";

    internal static List<OpenXmlElement> Build(string html, MainDocumentPart? main, HtmlConvertSettings? settings = null, OpenXmlPart? relationshipPart = null)
    {
        var document = parser.ParseDocument(string.Concat("<body>", html, "</body>"));
        var body = document.Body!;
        var elements = new List<OpenXmlElement>();
        var context = new WordBuildContext
        {
            MainPart = main,
            RelationshipPart = relationshipPart ?? main,
            ImageIndex = WordHtmlConverter.LastImageIndex(relationshipPart ?? main),
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
        PlaceLeftoverBookmarks(elements, context);

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
        if (!HtmlSegmentParser.HasStackToRecurse())
        {
            HtmlSegmentParser.ReportTooDeep(node, ctx.Settings);
            foreach (var textNode in HtmlSegmentParser.TextNodes(node))
            {
                var text = inPre ? textNode.Data : HtmlSegmentParser.CollapseWhitespace(textNode.Data, ctx.LastWasSpace);
                if (text.Length > 0)
                {
                    AddTextRun(text, format, ctx);
                    ctx.LastWasSpace = !inPre && text[^1] == ' ';
                }
            }

            return;
        }

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
        if (HtmlSegmentParser.IsHiddenElement(element, tag, styleDeclarations, context.Settings))
        {
            return;
        }

        var wasPre = inPre;
        inPre = HtmlSegmentParser.ApplyWhiteSpace(styleDeclarations, ref newFormat, inPre);

        // <pre> is an ordinary block whose text happens to be preformatted. It used to return from
        // the switch below, ahead of everything a block is given further down: its class was never
        // looked up in the style map, and its margins, page breaks and id were dropped.
        if (tag == "pre")
        {
            inPre = true;
        }

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
                // With nowhere to put an image there is no point fetching or decoding one: its alt
                // text stands in for it either way, so that is all that is read.
                ImageData? imageData = null;
                if (context.RelationshipPart == null)
                {
                    Diagnostic.UnsupportedElement(context.Settings, tag, NoMainPartForImages);
                }
                else
                {
                    imageData = ImageResolver.Resolve(element, context.Settings);
                }

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
                    context.ImageIndex++;
                    context.CurrentRuns.Add(WordHtmlConverter.BuildImageRun(context.RelationshipPart!, imageData, context.ImageIndex));
                    // An image is content, so a space after it does not fold into one before it.
                    context.LastWasSpace = false;
                }

                return;
            }
            case "svg":
            {
                if (context.RelationshipPart == null)
                {
                    Diagnostic.UnsupportedElement(context.Settings, tag, NoMainPartForImages);
                    return;
                }

                var svgData = HtmlSegmentParser.ParseSvgElement(element);
                context.ImageIndex++;
                context.CurrentRuns.Add(WordHtmlConverter.BuildImageRun(context.RelationshipPart, svgData, context.ImageIndex));
                context.LastWasSpace = false;
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
                // A table carries no pageBreakBefore of its own, so a break owed at this point has
                // to be spent on an empty paragraph ahead of it. That is what Word itself writes.
                if (context.PendingPageBreak)
                {
                    context.PendingPageBreak = false;
                    elements.Add(new Paragraph(new ParagraphProperties(new PageBreakBefore())));
                }

                // Word reads two tables with nothing between them as one table: it joins the rows
                // and lays them out on a single grid. An empty paragraph is what keeps them apart,
                // and is what Word itself puts there.
                if (elements.Count > 0 &&
                    elements[^1] is Table)
                {
                    elements.Add(new Paragraph());
                }

                BuildTable(element, format, elements, context);
                return;
            // Lists dispatch before the block handling below, so their class mapping is applied here
            // instead. Without this a class on <ul>/<ol>/<li> is never looked up at all and items
            // fall back to ListParagraph — which the fallback is written to allow, applying itself
            // only when nothing else set a style.
            case "ul" or "ol":
            {
                var ambient = ApplyAmbientParagraphStyle(element, context);
                BuildList(element, tag, newFormat, elements, context, inPre);
                RestoreAmbientParagraphStyle(context, ambient);
                return;
            }
            case "li":
            {
                var ambient = ApplyAmbientParagraphStyle(element, context);
                BuildListItem(element, newFormat, elements, context, inPre, listIndex, styleDeclarations);
                RestoreAmbientParagraphStyle(context, ambient);
                return;
            }
            case "a":
            {
                // <a name="top"> and <a id="top"> are how html names a place to link to.
                var anchorBookmark = OpenInlineBookmark(element, context);
                BuildAnchor(element, format, newFormat, elements, context, inPre);
                CloseInlineBookmark(anchorBookmark, context);
                return;
            }
            case "q":
            {
                AddTextRun("\u201C", format, context);
                ProcessChildren(element, newFormat, elements, context, inPre);
                AddTextRun("\u201D", format, context);
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
                    CanHoldFootnotes(context))
                {
                    // ReSharper disable once RedundantSuppressNullableWarningExpression
                    context.CurrentRuns.Add(BuildFootnoteRun(context, title!));
                }

                return;
            }
        }

        var isBlock = HtmlSegmentParser.IsBlockElement(tag);
        var pageBreakBefore = false;
        var pageBreakAfter = false;
        // What an enclosing block owed before this one, restored once its children are done so a
        // sibling does not inherit it — see AmbientParagraphStyleId.
        var ambientParagraphStyleId = context.AmbientParagraphStyleId;
        var ambientParagraphFormat = context.AmbientParagraphFormat;

        if (isBlock)
        {
            // A block's border belongs to its paragraph, which ParagraphFormatState draws as pBdr.
            // Left on the run format as well it was drawn a second time around the text itself, a
            // box inside the box, and handed down to every block nested inside. css does not
            // inherit border, so the runs go back to whatever an enclosing inline element set.
            newFormat.Border = format.Border;

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

            // The first block child of a list item continues the item's own paragraph rather than
            // starting a new one, which is where the bullet sits: <li><p>x</p></li> is one bulleted
            // line, not a bullet stranded above an unbulleted one. Later children of the same item
            // do start new paragraphs, matching a browser putting only the first line on the marker.
            if (!AtListItemStart(context))
            {
                FlushParagraph(elements, context);
            }

            // Over what the enclosing block owes, and owed in turn to the blocks inside this one.
            // Restored on the way out, below.
            pendingFormat = ParagraphFormatState.Combine(ambientParagraphFormat, pendingFormat);
            if (pendingFormat != null)
            {
                context.ParagraphFormat = pendingFormat;
                context.AmbientParagraphFormat = pendingFormat.ForDescendants();
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
                    // Owed to every paragraph inside this block, not just the one it produces
                    // itself. Restored on the way out, below.
                    context.ParagraphStyleId = paraStyle;
                    context.AmbientParagraphStyleId = paraStyle;
                }

                if (runStyle != null)
                {
                    newFormat.RunStyleId = runStyle;
                }
            }

            if (pageBreakBefore)
            {
                context.PendingPageBreak = true;
            }
        }
        else if (context.StyleMap != null &&
                 element.ClassList.Length > 0)
        {
            // Inline elements: check for character style
            var (_, runStyle) = WordStyleLookup.LookupClasses(element, context.StyleMap);
            if (runStyle != null)
            {
                newFormat.RunStyleId = runStyle;
            }
        }

        // Add bookmark for elements with id attribute. An inline element sits inside a paragraph
        // already, so its markers go straight in around its runs. A block's wait for a paragraph.
        BookmarkStart? blockBookmark = null;
        string? inlineBookmark = null;
        if (isBlock)
        {
            var elementId = element.GetAttribute("id") ?? element.GetAttribute("name");
            if (elementId != null)
            {
                blockBookmark = NewBookmarkStart(elementId, context);
                context.PendingBookmarks.Add(blockBookmark);
            }
        }
        else
        {
            inlineBookmark = OpenInlineBookmark(element, context);
        }

        var elementCountBeforeChildren = elements.Count;
        ProcessChildren(element, newFormat, elements, context, inPre);
        if (inPre && !wasPre)
        {
            TrimClosingLineBreak(context);
        }

        // A blockquote's cite becomes a footnote at its end. It is still a block like any other, so
        // this sits inside the ordinary handling rather than returning ahead of it, which is what
        // cost a cited blockquote its class, its margins and its id.
        if (tag == "blockquote" &&
            CanHoldFootnotes(context) &&
            element.GetAttribute("cite") is { Length: > 0 } cite)
        {
            context.CurrentRuns.Add(BuildFootnoteRun(context, cite));
        }

        CloseInlineBookmark(inlineBookmark, context);
        if (blockBookmark != null)
        {
            CloseBlockBookmark(blockBookmark, elements, context);
        }

        if (isBlock)
        {
            // A text block that produced no content at all was written empty in the source. That is a
            // blank line, and an empty paragraph is its only representation in Word, so emit it rather
            // than dropping it. Container blocks are excluded: an empty <ul> or <table> means "no
            // content", not "a blank line".
            var emitEmpty = IsTextBlock(tag) &&
                            context.CurrentRuns.Count == 0 &&
                            elements.Count == elementCountBeforeChildren;
            FlushParagraph(elements, context, emitEmpty);

            // Out of this block's scope: hand back what the enclosing one owed, so the style does not
            // leak onto a sibling. The flush above already ran, so ParagraphStyleId is realigned too.
            if (context.AmbientParagraphStyleId != ambientParagraphStyleId)
            {
                context.AmbientParagraphStyleId = ambientParagraphStyleId;
                context.ParagraphStyleId = ambientParagraphStyleId;
            }

            if (context.AmbientParagraphFormat != ambientParagraphFormat)
            {
                context.AmbientParagraphFormat = ambientParagraphFormat;
                context.ParagraphFormat = ambientParagraphFormat;
            }

            if (pageBreakAfter)
            {
                context.PendingPageBreak = true;
            }
        }
    }

    static BookmarkStart NewBookmarkStart(string name, WordBuildContext context)
    {
        context.BookmarkId++;
        return new()
        {
            Id = context.BookmarkId.ToString(),
            Name = name
        };
    }

    static string? OpenInlineBookmark(IElement element, WordBuildContext context)
    {
        var name = element.GetAttribute("id") ?? element.GetAttribute("name");
        if (name == null)
        {
            return null;
        }

        var start = NewBookmarkStart(name, context);
        context.CurrentRuns.Add(start);
        return start.Id!.Value;
    }

    static void CloseInlineBookmark(string? bookmarkId, WordBuildContext context)
    {
        if (bookmarkId == null)
        {
            return;
        }

        context.CurrentRuns.Add(
            new BookmarkEnd
            {
                Id = bookmarkId
            });
    }

    // Closes a block's bookmark where its content ended, without making a paragraph to do it in.
    static void CloseBlockBookmark(BookmarkStart start, List<OpenXmlElement> elements, WordBuildContext context)
    {
        var end = new BookmarkEnd
        {
            Id = start.Id!.Value
        };

        // The block's own text is still open, so the bookmark closes after it.
        if (context.CurrentRuns.Count > 0)
        {
            context.CurrentRuns.Add(end);
            return;
        }

        // Still waiting, so nothing was built inside the block: the pair stays together.
        if (context.PendingBookmarks.Contains(start))
        {
            context.PendingBookmarks.Add(end);
            return;
        }

        if (LastParagraph(elements) is { } last)
        {
            last.Append(end);
            return;
        }

        context.PendingBookmarks.Add(end);
    }

    // The paragraph the content built so far ends in. After a table that is the one closing its
    // last cell, which every cell has.
    static Paragraph? LastParagraph(List<OpenXmlElement> elements)
    {
        if (elements.Count == 0)
        {
            return null;
        }

        if (elements[^1] is Paragraph paragraph)
        {
            return paragraph;
        }

        return elements[^1].Descendants<Paragraph>().LastOrDefault();
    }

    // Markers that never met a paragraph, such as a bookmarked block with nothing in it at the end
    // of the html. They go on the last paragraph there is, or on one of their own when there is
    // none.
    static void PlaceLeftoverBookmarks(List<OpenXmlElement> elements, WordBuildContext context)
    {
        if (context.PendingBookmarks.Count == 0)
        {
            return;
        }

        var paragraph = LastParagraph(elements);
        if (paragraph == null)
        {
            paragraph = new();
            elements.Add(paragraph);
        }

        foreach (var marker in context.PendingBookmarks)
        {
            paragraph.Append(marker);
        }

        context.PendingBookmarks.Clear();
    }

    // Sets the paragraph style an element's class maps to, as owed by everything inside it, and hands
    // back what was owed before so RestoreAmbientParagraphStyle can put it back. Returns the previous
    // value unchanged when the element carries no mapped paragraph class.
    static string? ApplyAmbientParagraphStyle(IElement element, WordBuildContext context)
    {
        var previous = context.AmbientParagraphStyleId;
        if (context.StyleMap == null ||
            element.ClassList.Length == 0)
        {
            return previous;
        }

        var (paraStyle, _) = WordStyleLookup.LookupClasses(element, context.StyleMap);
        if (paraStyle != null)
        {
            context.ParagraphStyleId = paraStyle;
            context.AmbientParagraphStyleId = paraStyle;
        }

        return previous;
    }

    static void RestoreAmbientParagraphStyle(WordBuildContext context, string? previous)
    {
        if (context.AmbientParagraphStyleId == previous)
        {
            return;
        }

        context.AmbientParagraphStyleId = previous;
        context.ParagraphStyleId = previous;
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

    // A footnote reference is only valid in the document body. A header or footer cannot hold one,
    // so there the title or cite is left out rather than written as a reference Word rejects.
    static bool CanHoldFootnotes(WordBuildContext context) =>
        context.MainPart != null &&
        context.RelationshipPart == context.MainPart;

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

        // Every run that reaches the paragraph moves the fold state, not only the ones that came
        // from a text node. Alt text, a checkbox glyph and a list marker are written here too, and
        // while they left the state alone the next text node folded against whatever came before
        // them: "see <img alt="x"> below" lost the space after the alt text, and "☑ " kept a
        // second space ahead of its label. The segment path reads the last segment, so it never
        // had this.
        ctx.LastWasSpace = text.Length > 0 && text[^1] == ' ';

        AppendText(run, ApplyTextTransform(text, format));
        ctx.CurrentRuns.Add(run);
    }

    static Text NewText(string value) =>
        new(value)
        {
            Space = SpaceProcessingModeValues.Preserve
        };

    static readonly char[] tabOrNewline = ['\t', '\n'];

    // Only white-space:pre gets a tab or a newline this far — the normal folding rules turn either
    // into a space before it reaches here. Word treats both as a plain space inside <w:t>, so a tab
    // has to be the <w:tab/> element to reach the next tab stop, and a newline the <w:br/> element
    // to break the line. Without the second a multi-line <pre> ran together on one line.
    internal static void AppendText(Run run, string value)
    {
        if (value.IndexOfAny(tabOrNewline) < 0)
        {
            run.Append(NewText(value));
            return;
        }

        var start = 0;
        while (true)
        {
            var index = value.IndexOfAny(tabOrNewline, start);
            if (index < 0)
            {
                break;
            }

            if (index > start)
            {
                run.Append(NewText(value.Substring(start, index - start)));
            }

            if (value[index] == '\t')
            {
                run.Append(new TabChar());
            }
            else
            {
                run.Append(new Break());
            }

            start = index + 1;
        }

        if (start < value.Length)
        {
            run.Append(NewText(value.Substring(start)));
        }
    }

    // The newline that ends preformatted text closes its last line rather than opening another, and
    // a browser draws nothing for it. As a <w:br/> it would be a blank line at the foot of the block.
    static void TrimClosingLineBreak(WordBuildContext context)
    {
        if (context.CurrentRuns.Count == 0 ||
            context.CurrentRuns[^1] is not Run { LastChild: Break lineBreak } run)
        {
            return;
        }

        lineBreak.Remove();
        if (run.ChildElements.All(_ => _ is RunProperties))
        {
            context.CurrentRuns.RemoveAt(context.CurrentRuns.Count - 1);
        }
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

        return WXmlChars.Strip(transformed);
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

    static bool IsTextBlock(string tag) =>
        tag is "p" or "div" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";

    // Inside a list item with nothing emitted for it yet beyond its marker.
    static bool AtListItemStart(WordBuildContext context) =>
        context.ListItemDepth > 0 &&
        context.CurrentRuns.Count == context.ListItemContentFloor;

    static void FlushParagraph(List<OpenXmlElement> elements, WordBuildContext context, bool emitEmpty = false)
    {
        context.LastWasSpace = false;

        // A <br> that ends a block is that block's end, not a line of its own. Left in, Word draws
        // an empty line after it, where a browser draws nothing. A paragraph that held only the
        // break is still a paragraph: <p><br></p> is how an editor writes a blank line.
        if (context.CurrentRuns.Count > 0 &&
            context.CurrentRuns[^1] is Run { LastChild: Break { Type: null } } closing &&
            closing.ChildElements.All(_ => _ is RunProperties or Break))
        {
            closing.LastChild!.Remove();
            if (closing.ChildElements.All(_ => _ is RunProperties))
            {
                context.CurrentRuns.RemoveAt(context.CurrentRuns.Count - 1);
            }

            emitEmpty = true;
        }

        if (context.CurrentRuns.Count == 0 && !emitEmpty)
        {
            context.HeadingLevel = 0;
            // Back to whatever an enclosing block owes rather than to nothing — see
            // AmbientParagraphStyleId. This is the flush that used to discard it, since entering a
            // block child flushes before any of that child's text has arrived.
            context.ParagraphStyleId = context.AmbientParagraphStyleId;
            context.ParagraphFormat = context.AmbientParagraphFormat;
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

        if (context.PendingBookmarks.Count > 0)
        {
            context.CurrentRuns.InsertRange(0, context.PendingBookmarks);
            context.PendingBookmarks.Clear();
        }

        var paragraph = WordHtmlConverter.BuildParagraph(context.CurrentRuns, context.ListNumId != null ? 0 : context.ListDepth);

        if (context.PendingPageBreak)
        {
            context.PendingPageBreak = false;
            paragraph.ParagraphProperties ??= new();
            paragraph.ParagraphProperties.PageBreakBefore = new();
        }

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
            AddParagraphProperty(
                paragraph.ParagraphProperties,
                new NumberingProperties(
                    new NumberingLevelReference
                    {
                        Val = context.ListIlvl ?? 0
                    },
                    new NumberingId
                    {
                        Val = context.ListNumId.Value
                    }));
            AddParagraphProperty(paragraph.ParagraphProperties, new ContextualSpacing());

            if (context.ListInside)
            {
                AddParagraphProperty(
                    paragraph.ParagraphProperties,
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
            AddParagraphProperty(paragraph.ParagraphProperties, new BiDi());
        }

        elements.Add(paragraph);
        context.CurrentRuns.Clear();
        context.ListDepth = 0;
        context.HeadingLevel = 0;
        // The next paragraph inside the same block is owed the same style, so this returns to the
        // enclosing block's rather than clearing outright — see AmbientParagraphStyleId.
        context.ParagraphStyleId = context.AmbientParagraphStyleId;
        context.ParagraphFormat = context.AmbientParagraphFormat;
        context.ParagraphRightToLeft = false;
        context.ListNumId = null;
        context.ListIlvl = null;
        context.ListInside = false;
    }

    // CT_PPrBase declares its children in a fixed order, and the order css reaches this builder in
    // is not that order: a background is read after an alignment, a border after both, and a list
    // paragraph carries its contextualSpacing before either indentation is worked out. Word repairs
    // what it is handed, so an appended child sat in the wrong place unnoticed until something
    // stricter read the document. Anything new emitted into a pPr belongs in this list.
    static readonly Type[] paragraphPropertyOrder =
    [
        typeof(ParagraphStyleId),
        typeof(PageBreakBefore),
        typeof(NumberingProperties),
        typeof(ParagraphBorders),
        typeof(Shading),
        typeof(BiDi),
        typeof(SpacingBetweenLines),
        typeof(Indentation),
        typeof(ContextualSpacing),
        typeof(Justification),
        typeof(TextDirection)
    ];

    static void AddParagraphProperty(ParagraphProperties props, OpenXmlElement property)
    {
        var position = Array.IndexOf(paragraphPropertyOrder, property.GetType());
        // An element the list does not name has no declared place to hold it to, so it goes last -
        // which is where appending would have put it anyway.
        var follower = position < 0
            ? null
            : props.ChildElements.FirstOrDefault(_ => Array.IndexOf(paragraphPropertyOrder, _.GetType()) > position);
        if (follower == null)
        {
            props.Append(property);
        }
        else
        {
            follower.InsertBeforeSelf(property);
        }
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
                spacing.Before = StyleParser.ClampUnsignedTwips(pf.MarginTopTwips.Value).ToString();
            }

            if (pf.MarginBottomTwips != null)
            {
                spacing.After = StyleParser.ClampUnsignedTwips(pf.MarginBottomTwips.Value).ToString();
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

            AddParagraphProperty(props, spacing);
        }

        if (pf.MarginLeftTwips != null ||
            pf.MarginRightTwips != null ||
            pf.TextIndentTwips != null)
        {
            var existingIndent = props.GetFirstChild<Indentation>();
            var indent = existingIndent ?? new Indentation();
            if (existingIndent == null)
            {
                AddParagraphProperty(props, indent);
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
            AddParagraphProperty(
                props,
                new Justification
                {
                    Val = pf.TextAlign.Value
                });
        }

        if (pf.BackgroundColor != null)
        {
            AddParagraphProperty(
                props,
                new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Fill = pf.BackgroundColor
                });
        }

        if (pf.WritingMode != null)
        {
            AddParagraphProperty(props, new BiDi());
            AddParagraphProperty(
                props,
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
            AddParagraphProperty(props, borders);
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

        if (!string.IsNullOrEmpty(href) && context.RelationshipPart != null &&
            LinkTarget.TryParseAbsolute(href, out var uri))
        {
            var rel = context.RelationshipPart.AddHyperlinkRelationship(uri, true, PartRelationshipId.Next(context.RelationshipPart, "rLink"));
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
