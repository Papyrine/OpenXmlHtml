namespace OpenXmlHtml;

/// <summary>
/// Seeds paragraph style definitions into a document so Word can apply named styles to it.
/// </summary>
public static class WordStyles
{
    // Heading level -> half-point font size. Descending sizes, bold; enough for Word to treat each as
    // the real built-in heading (it links Heading1..6 by styleId, so the ids must be exact).
    static readonly (int Level, int HalfPointSize)[] headings =
    [
        (1, 32),
        (2, 28),
        (3, 26),
        (4, 24),
        (5, 22),
        (6, 22)
    ];

    /// <summary>
    /// Ensures <paramref name="main" /> has a styles part carrying the common built-in paragraph styles a
    /// rich-text editor offers: <c>Normal</c>, <c>Heading1</c>–<c>Heading6</c>, and <c>ListParagraph</c>.
    /// </summary>
    /// <remarks>
    /// Word cannot apply a named paragraph style unless its definition already exists: creating one writes to
    /// <c>word/styles.xml</c>, a document-level part. Under <c>w:documentProtection w:edit="readOnly"</c> that
    /// part sits outside every editable range, so Word greys out the style gallery and the Heading buttons
    /// even inside a rich-text content control the user is allowed to edit. Seeding the definitions leaves
    /// Word merely referencing them, and the commands become available.
    ///
    /// Idempotent: a style already present (by <c>w:styleId</c>) is left untouched, so seeding never disturbs
    /// styles a template already defines. The styles part is created with a deterministic relationship id, so
    /// output stays byte-reproducible.
    /// </remarks>
    public static void EnsureStyleDefinitions(MainDocumentPart main)
    {
        var styles = EnsureStylesPart(main).Styles!;

        Ensure(styles, "Normal", NormalStyle);
        foreach (var (level, halfPointSize) in headings)
        {
            Ensure(styles, $"Heading{level}", () => HeadingStyle(level, halfPointSize));
        }

        Ensure(styles, "ListParagraph", ListParagraphStyle);
    }

    static StyleDefinitionsPart EnsureStylesPart(MainDocumentPart main)
    {
        var part = main.StyleDefinitionsPart;
        if (part != null)
        {
            part.Styles ??= new();
            return part;
        }

        part = main.AddNewPart<StyleDefinitionsPart>(PartRelationshipId.Next(main, "rStyles"));
        part.Styles = new();
        return part;
    }

    static void Ensure(Styles styles, string styleId, Func<Style> factory)
    {
        if (styles.Elements<Style>().Any(_ => _.StyleId?.Value == styleId))
        {
            return;
        }

        styles.AppendChild(factory());
    }

    static Style NormalStyle() =>
        new(
            new StyleName
            {
                Val = "Normal"
            },
            new PrimaryStyle())
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true
        };

    // Child order follows CT_Style's xsd:sequence: name, basedOn, next, uiPriority, qFormat, pPr, rPr.
    static Style HeadingStyle(int level, int halfPointSize) =>
        new(
            new StyleName
            {
                Val = $"heading {level}"
            },
            new BasedOn
            {
                Val = "Normal"
            },
            new NextParagraphStyle
            {
                Val = "Normal"
            },
            new UIPriority
            {
                Val = 9
            },
            new PrimaryStyle(),
            new StyleParagraphProperties(
                new KeepNext(),
                new KeepLines(),
                new SpacingBetweenLines
                {
                    Before = "240",
                    After = "0"
                },
                new OutlineLevel
                {
                    Val = level - 1
                }),
            new StyleRunProperties(
                new Bold(),
                new FontSize
                {
                    Val = halfPointSize.ToString(CultureInfo.InvariantCulture)
                }))
        {
            Type = StyleValues.Paragraph,
            StyleId = $"Heading{level}"
        };

    static Style ListParagraphStyle() =>
        new(
            new StyleName
            {
                Val = "List Paragraph"
            },
            new BasedOn
            {
                Val = "Normal"
            },
            new UIPriority
            {
                Val = 34
            },
            new PrimaryStyle(),
            new StyleParagraphProperties(
                new Indentation
                {
                    Left = "720"
                },
                new ContextualSpacing()))
        {
            Type = StyleValues.Paragraph,
            StyleId = "ListParagraph"
        };
}
