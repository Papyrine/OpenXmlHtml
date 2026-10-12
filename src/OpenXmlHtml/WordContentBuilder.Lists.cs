static partial class WordContentBuilder
{
    static void BuildList(IElement element, string tag, in FormatState newFormat, List<OpenXmlElement> elements, WordBuildContext context, bool inPre)
    {
        FlushParagraph(elements, context);
        var isReversed = tag == "ol" && element.HasAttribute("reversed");

        if (context.MainPart == null || isReversed)
        {
            // Text markers: BuildListItem writes each one from the list's own attributes.
            ProcessChildren(element, newFormat, elements, context, inPre);
        }
        else
        {
            BuildRealNumberedList(element, tag, newFormat, elements, context, inPre);
        }

        FlushParagraph(elements, context);
    }

    static void BuildRealNumberedList(IElement element, string tag, in FormatState newFormat, List<OpenXmlElement> elements, WordBuildContext context, bool inPre)
    {
        var isOrdered = tag == "ol";
        var part = WordNumberingBuilder.EnsureNumberingPart(context.MainPart!);
        var numbering = part.Numbering!;

        var typeAttr = element.GetAttribute("type");
        var listDeclarations = element.GetAttribute("style") is { } listStyle
            ? StyleParser.Parse(listStyle)
            : null;
        var listStyleCss = listDeclarations?.GetValueOrDefault("list-style-type");
        var inside = listDeclarations != null &&
                     listDeclarations.TryGetValue("list-style-position", out var pos) &&
                     pos.Equals("inside", StringComparison.OrdinalIgnoreCase);
        var noMarker = string.Equals(listStyleCss, "none", StringComparison.OrdinalIgnoreCase);

        if (noMarker)
        {
            var ilvlNoMarker = context.ListStack.Count > 0 ? Math.Min(context.ListStack.Peek().Ilvl + 1, 8) : 0;
            context.ListStack.Push((0, ilvlNoMarker, isOrdered, inside, true));
            ProcessChildren(element, newFormat, elements, context, inPre);
            context.ListStack.Pop();
            return;
        }

        var format = WordNumberingBuilder.ParseListStyleType(typeAttr, listStyleCss, isOrdered);

        int abstractNumId;
        if (format == NumberFormatValues.Bullet)
        {
            if (context.BulletAbstractNumId == null)
            {
                var id = context.NextNumId++;
                context.BulletAbstractNumId = WordNumberingBuilder.CreateBulletAbstractNum(numbering, id);
            }

            abstractNumId = context.BulletAbstractNumId.Value;
        }
        else if (!context.OrderedAbstractNumIds.TryGetValue(format, out abstractNumId))
        {
            var id = context.NextNumId++;
            abstractNumId = WordNumberingBuilder.CreateOrderedAbstractNum(numbering, id, format);
            context.OrderedAbstractNumIds[format] = abstractNumId;
        }

        // An ordered list always says where it starts. Instances that share a definition would
        // otherwise carry on from one another, so the override is what makes each list its own.
        int? startOverride = null;
        if (format != NumberFormatValues.Bullet)
        {
            startOverride = 1;
            var startAttr = element.GetAttribute("start");
            if (startAttr != null &&
                int.TryParse(startAttr, out var startVal))
            {
                startOverride = startVal;
            }
        }

        var numId = context.NextNumId++;
        var ilvl = context.ListStack.Count > 0 ? Math.Min(context.ListStack.Peek().Ilvl + 1, 8) : 0;
        WordNumberingBuilder.AddNumberingInstance(numbering, numId, abstractNumId, startOverride, ilvl);
        var parentInside = context.ListStack.Count > 0 && context.ListStack.Peek().Inside;
        context.ListStack.Push((numId, ilvl, isOrdered, inside || parentInside, false));
        ProcessChildren(element, newFormat, elements, context, inPre);
        context.ListStack.Pop();
    }

    static void BuildListItem(IElement element, in FormatState newFormat, List<OpenXmlElement> elements, WordBuildContext context, bool inPre, int listIndex, Dictionary<string, string>? declarations)
    {
        FlushParagraph(elements, context);

        // The item's own paragraph formatting and page break. Lists dispatch ahead of the ordinary
        // block handling, so nothing else reads them: text-align or a margin on an <li> was dropped.
        if (declarations != null)
        {
            var itemFormat = ParagraphFormatState.ParseFrom(declarations);
            if (itemFormat.HasProperties)
            {
                context.ParagraphFormat = itemFormat;
            }

            if (IsPageBreak(declarations, "page-break-before") ||
                IsPageBreak(declarations, "break-before"))
            {
                context.PendingPageBreak = true;
            }
        }

        if (context.ListStack.Count > 0)
        {
            var (numId, ilvl, _, inside, noMarker) = context.ListStack.Peek();
            if (noMarker)
            {
                // list-style-type:none — render as plain paragraph with indentation
                context.ListDepth = ilvl + 1;
            }
            else
            {
                context.ListNumId = numId;
                context.ListIlvl = ilvl;
                context.ListInside = inside;
            }
        }
        else
        {
            // Fallback: text prefix when no MainDocumentPart
            var parent = element.ParentElement?.LocalName;
            var depth = HtmlSegmentParser.GetListDepth(element);
            context.ListDepth = depth;

            if (parent == "ol")
            {
                AddTextRun(HtmlSegmentParser.OrderedMarker(element, listIndex), newFormat, context);
                context.ListMarker = (Run)context.CurrentRuns[^1];
            }
            else
            {
                var bullet = depth switch
                {
                    0 => "\u25CF",
                    1 => "\u25CB",
                    _ => "\u25A0"
                };
                AddTextRun($"{bullet} ", newFormat, context);
                context.ListMarker = (Run)context.CurrentRuns[^1];
            }
        }

        var outerFloor = context.ListItemContentFloor;
        context.ListItemContentFloor = context.CurrentRuns.Count;
        context.ListItemDepth++;
        ProcessChildren(element, newFormat, elements, context, inPre);
        context.ListItemDepth--;
        context.ListItemContentFloor = outerFloor;

        // Decremented first, so an empty <li></li> clears the list state here rather than leaking
        // it onto whatever follows the list.
        FlushParagraph(elements, context);
    }
}
