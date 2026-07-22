[TestFixture]
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
    public void BulletLevelsUseFontGlyphsNotUnicodeBullets()
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());

        WordHtmlConverter.ToElements("<ul><li>a</li></ul>", main);

        var abs = GetBulletAbstractNum(main);

        Assert.Multiple(() =>
        {
            Assert.That(ReadLevel(abs, 0), Is.EqualTo(("\uF0B7", "Symbol")));
            Assert.That(ReadLevel(abs, 1), Is.EqualTo(("o", "Courier New")));
            Assert.That(ReadLevel(abs, 2), Is.EqualTo(("\uF0A7", "Wingdings")));
            Assert.That(ReadLevel(abs, 3), Is.EqualTo(("\uF0B7", "Symbol")));
            Assert.That(ReadLevel(abs, 4), Is.EqualTo(("o", "Courier New")));
            Assert.That(ReadLevel(abs, 5), Is.EqualTo(("\uF0A7", "Wingdings")));
        });
    }

    // Description html arrives <p>-wrapped often enough that <li><p>x</p></li> has to stay a list.
    // The block child continues the item's own paragraph rather than starting one after it, which
    // would leave the marker stranded on a line of its own.
    [Test]
    public void ListItemWithABlockChildKeepsItsMarker()
    {
        var wrapped = WordHtmlConverter.ToElements("<ul><li><p>x</p></li></ul>");
        var bare = WordHtmlConverter.ToElements("<ul><li>x</li></ul>");

        Assert.That(wrapped.OfType<Paragraph>().Count(), Is.EqualTo(1));
        Assert.That(
            wrapped.OfType<Paragraph>().Single().InnerText,
            Is.EqualTo(bare.OfType<Paragraph>().Single().InnerText));
    }

    // Only the first line sits on the marker, so later children still start their own paragraphs.
    [Test]
    public void ListItemWithSeveralBlockChildrenOnlyMarksTheFirst()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("<ul><li><p>x</p><p>y</p></li></ul>")
            .OfType<Paragraph>()
            .ToList();

        Assert.That(paragraphs, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(paragraphs[0].InnerText, Does.EndWith("x").And.Not.EqualTo("x"));
            Assert.That(paragraphs[1].InnerText, Is.EqualTo("y"));
        });
    }

    // Text before the block already occupies the marker's line, so the block starts a new one.
    [Test]
    public void ListItemWithTextBeforeABlockChildSplitsAfterTheText()
    {
        var paragraphs = WordHtmlConverter
            .ToElements("<ul><li>lead<p>x</p></li></ul>")
            .OfType<Paragraph>()
            .ToList();

        Assert.That(paragraphs, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(paragraphs[0].InnerText, Does.EndWith("lead"));
            Assert.That(paragraphs[1].InnerText, Is.EqualTo("x"));
        });
    }

    [Test]
    public void ListParagraphsHaveContextualSpacing()
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

        Assert.That(listParagraphs, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            foreach (var p in listParagraphs)
            {
                Assert.That(
                    p.ParagraphProperties!.GetFirstChild<ContextualSpacing>(),
                    Is.Not.Null,
                    "list paragraph must set w:contextualSpacing so consecutive list items render tight");
            }
        });
    }
}
