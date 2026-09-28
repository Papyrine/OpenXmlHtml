public class WordListBulletGlyphTests
{
    static AbstractNum GetBulletAbstractNum(MainDocumentPart main) =>
        main.NumberingDefinitionsPart!.Numbering!
            .Elements<AbstractNum>()
            .Single(a =>
                a.Elements<Level>().FirstOrDefault(_ => _.LevelIndex?.Value == 0) is { } l &&
                l.NumberingFormat?.Val?.Value == NumberFormatValues.Bullet);

    static (string glyph, string font) ReadLevel(AbstractNum abs, int ilvl)
    {
        var level = abs.Elements<Level>().Single(_ => _.LevelIndex?.Value == ilvl);
        var glyph = level.LevelText!.Val!.Value!;
        var fonts = level.NumberingSymbolRunProperties!.GetFirstChild<RunFonts>()!;
        return (glyph, fonts.Ascii!.Value!);
    }

    [Test]
    public async Task BulletLevelsUseFontGlyphsNotUnicodeBullets()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());

        WordHtmlConverter.ToElements("<ul><li>a</li></ul>", main);

        var abs = GetBulletAbstractNum(main);

        using (Assert.Multiple())
        {
            await Assert.That(ReadLevel(abs, 0)).IsEqualTo(("\uF0B7", "Symbol"));
            await Assert.That(ReadLevel(abs, 1)).IsEqualTo(("o", "Courier New"));
            await Assert.That(ReadLevel(abs, 2)).IsEqualTo(("\uF0A7", "Wingdings"));
            await Assert.That(ReadLevel(abs, 3)).IsEqualTo(("\uF0B7", "Symbol"));
            await Assert.That(ReadLevel(abs, 4)).IsEqualTo(("o", "Courier New"));
            await Assert.That(ReadLevel(abs, 5)).IsEqualTo(("\uF0A7", "Wingdings"));
        }
    }

    // Description html arrives <p>-wrapped often enough that <li><p>x</p></li> has to stay a list.
    // The block child continues the item's own paragraph rather than starting one after it, which
    // would leave the marker stranded on a line of its own.
    [Test]
    public async Task ListItemWithABlockChildKeepsItsMarker()
    {
        var wrapped = WordHtmlConverter.ToElements("<ul><li><p>x</p></li></ul>");
        var bare = WordHtmlConverter.ToElements("<ul><li>x</li></ul>");

        await Assert.That(wrapped.OfType<Paragraph>().Count()).IsEqualTo(1);
        await Assert.That(wrapped.OfType<Paragraph>().Single().InnerText).IsEqualTo(bare.OfType<Paragraph>().Single().InnerText);
    }

    // Only the first line sits on the marker, so later children still start their own paragraphs.
    [Test]
    public async Task ListItemWithSeveralBlockChildrenOnlyMarksTheFirst()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("<ul><li><p>x</p><p>y</p></li></ul>")
            .OfType<Paragraph>()
            .ToList();

        await Assert.That(paragraphs).Count().IsEqualTo(2);
        using (Assert.Multiple())
        {
            await Assert.That(paragraphs[0].InnerText).EndsWith("x").And.IsNotEqualTo("x");
            await Assert.That(paragraphs[1].InnerText).IsEqualTo("y");
        }
    }

    // Text before the block already occupies the marker's line, so the block starts a new one.
    [Test]
    public async Task ListItemWithTextBeforeABlockChildSplitsAfterTheText()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("<ul><li>lead<p>x</p></li></ul>")
            .OfType<Paragraph>()
            .ToList();

        await Assert.That(paragraphs).Count().IsEqualTo(2);
        using (Assert.Multiple())
        {
            await Assert.That(paragraphs[0].InnerText).EndsWith("lead");
            await Assert.That(paragraphs[1].InnerText).IsEqualTo("x");
        }
    }

    [Test]
    public async Task ListParagraphsHaveContextualSpacing()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());

        var elements = WordHtmlConverter.ToElements(
            """
            <ul>
              <li>Bullet</li>
            </ul>
            <ol>
              <li>Numbered</li>
            </ol>
            """,
            main);

        var listParagraphs = elements
            .OfType<Paragraph>()
            .Where(p => p.ParagraphProperties?.GetFirstChild<NumberingProperties>() != null)
            .ToList();

        await Assert.That(listParagraphs).Count().IsEqualTo(2);
        using (Assert.Multiple())
        {
            foreach (var p in listParagraphs)
            {
                await Assert.That(p.ParagraphProperties!.GetFirstChild<ContextualSpacing>()).IsNotNull().Because("list paragraph must set w:contextualSpacing so consecutive list items render tight");
            }
        }
    }
}
