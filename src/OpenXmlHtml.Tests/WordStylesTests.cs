public class WordStylesTests
{
    static readonly string[] expectedStyleIds =
    [
        "Normal", "Heading1", "Heading2", "Heading3", "Heading4", "Heading5", "Heading6", "ListParagraph"
    ];

    [Test]
    public async Task EnsureStyleDefinitions_SeedsExpectedParagraphStyles()
    {
        using var document = SeedNewDocument();
        var styles = document.MainDocumentPart!.StyleDefinitionsPart!.Styles!;

        await Assert.That(StyleIds(styles)).IsEquivalentTo(expectedStyleIds.Cast<string?>());

        // Every seeded style is a paragraph style — Word links the Heading buttons and the style gallery
        // to paragraph styleIds.
        foreach (var style in styles.Elements<Style>())
        {
            await Assert.That(style.Type?.Value).IsEqualTo(StyleValues.Paragraph).Because($"{style.StyleId?.Value} is not a paragraph style");
        }

        var normal = styles.Elements<Style>().Single(_ => _.StyleId?.Value == "Normal");
        await Assert.That(normal.Default?.Value).IsTrue();
    }

    [Test]
    public async Task EnsureStyleDefinitions_HeadingsCarryBuiltInNamesAndOutlineLevels()
    {
        using var document = SeedNewDocument();
        var styles = document.MainDocumentPart!.StyleDefinitionsPart!.Styles!;

        for (var level = 1; level <= 6; level++)
        {
            var heading = styles.Elements<Style>().Single(_ => _.StyleId?.Value == $"Heading{level}");
            // Word maps a style to its built-in heading by the "heading N" name + outline level, not the id alone.
            await Assert.That(heading.StyleName?.Val?.Value).IsEqualTo($"heading {level}");
            await Assert.That(heading.StyleParagraphProperties?.OutlineLevel?.Val?.Value).IsEqualTo(level - 1);
        }
    }

    [Test]
    public async Task EnsureStyleDefinitions_IsIdempotent()
    {
        using var document = SeedNewDocument();
        var main = document.MainDocumentPart!;

        WordStyles.EnsureStyleDefinitions(main);
        WordStyles.EnsureStyleDefinitions(main);

        await Assert.That(main.StyleDefinitionsPart!.Styles!.Elements<Style>().Count()).IsEqualTo(expectedStyleIds.Length);
    }

    [Test]
    public async Task EnsureStyleDefinitions_UsesDeterministicRelationshipId()
    {
        using var document = SeedNewDocument();
        var main = document.MainDocumentPart!;

        await Assert.That(main.GetIdOfPart(main.StyleDefinitionsPart!)).IsEqualTo("rStyles");
    }

    /// <summary>
    /// Seeding into a document that already defines some of these styles must leave those definitions
    /// untouched — a template's own Heading1 wins over the seed — and must not disturb unrelated styles.
    /// </summary>
    [Test]
    public async Task EnsureStyleDefinitions_PreservesExistingStyles()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new(
            new Style(
                new StyleName { Val = "heading 1" },
                // A marker so we can prove the existing Heading1 survives rather than being replaced.
                new UIPriority { Val = 999 })
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1"
            },
            new Style(new StyleName { Val = "Custom" })
            {
                Type = StyleValues.Paragraph,
                StyleId = "Custom"
            });

        WordStyles.EnsureStyleDefinitions(main);

        var styles = main.StyleDefinitionsPart!.Styles!;
        await Assert.That(styles.Elements<Style>().Count(_ => _.StyleId?.Value == "Heading1")).IsEqualTo(1);
        await Assert.That(styles.Elements<Style>().Single(_ => _.StyleId?.Value == "Heading1").GetFirstChild<UIPriority>()!.Val!.Value).IsEqualTo(999);
        await Assert.That(styles.Elements<Style>().Any(_ => _.StyleId?.Value == "Custom")).IsTrue();
        await Assert.That(expectedStyleIds.Except(StyleIds(styles).OfType<string>())).IsEmpty();
    }

    static WordprocessingDocument SeedNewDocument()
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordStyles.EnsureStyleDefinitions(main);
        return document;
    }

    static IEnumerable<string?> StyleIds(Styles styles) =>
        styles.Elements<Style>().Select(_ => _.StyleId?.Value);
}
