/// <summary>
/// <c>w:numbering</c> is a strict sequence: the <c>w:numPicBullet</c> elements, then the
/// <c>w:abstractNum</c> elements, then the <c>w:num</c> elements, then <c>w:numIdMacAtCleanup</c>.
/// A list converted into a numbering part that already holds any of those has to put its
/// definition and its instance inside that sequence rather than at the end.
/// </summary>
public class WordNumberingSequenceTests
{
    const string bulleted = "<ul><li>a</li><li>b</li></ul>";
    const string ordered = """<ol start="3"><li>c</li><li>d</li></ol>""";
    const string nested = "<ul><li>e<ol><li>f<ul><li>g</li></ul></li></ol></li></ul>";
    const string several = """<ul><li>h</li></ul><p>text</p><ol type="i"><li>i</li></ol><ol><li>j</li></ol><ul><li>k</li></ul>""";

    static readonly string[] sequence = ["numPicBullet", "abstractNum", "num", "numIdMacAtCleanup"];

    [Test]
    [Arguments(bulleted, 2)]
    [Arguments(ordered, 2)]
    [Arguments(nested, 3)]
    [Arguments(several, 4)]
    public async Task ListIntoExistingNumberingKeepsTheSequence(string html, int listItems)
    {
        using var stream = Document(pictureBullet: true, definitions: true, cleanup: true);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;

        WordHtmlConverter.AppendHtml(main.Document!.Body!, html, main);

        await AssertSequence(document, listItems);
        await AssertExistingKept(main, pictureBullet: true, definitions: true, cleanup: true);
    }

    /// <summary>
    /// Each combination of what the part starts with moves where a new element has to go: with no
    /// <c>w:num</c> the definition has nothing to go in ahead of, and with no <c>w:abstractNum</c>
    /// nothing to follow.
    /// </summary>
    [Test]
    [MatrixDataSource]
    public async Task EveryStartingPartKeepsTheSequence(
        [Matrix(true, false)] bool pictureBullet,
        [Matrix(true, false)] bool definitions,
        [Matrix(true, false)] bool cleanup)
    {
        using var stream = Document(pictureBullet, definitions, cleanup);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;

        WordHtmlConverter.AppendHtml(main.Document!.Body!, bulleted + ordered + nested + several, main);

        await AssertSequence(document, 11);
        await AssertExistingKept(main, pictureBullet, definitions, cleanup);
    }

    [Test]
    public async Task SeveralCallsKeepTheSequence()
    {
        using var stream = Document(pictureBullet: true, definitions: true, cleanup: true);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;
        var body = main.Document!.Body!;

        WordHtmlConverter.AppendHtml(body, bulleted, main);
        WordHtmlConverter.AppendHtml(body, ordered, main);
        WordHtmlConverter.AppendHtml(body, nested, main);

        await AssertSequence(document, 7);
    }

    [Test]
    public async Task NumberingSessionKeepsTheSequence()
    {
        using var stream = Document(pictureBullet: true, definitions: true, cleanup: true);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;
        var body = main.Document!.Body!;
        var settings = new HtmlConvertSettings
        {
            NumberingSession = new()
        };

        body.Append(WordHtmlConverter.ToElements(bulleted, main, settings));
        body.Append(WordHtmlConverter.ToElements(ordered, main, settings));
        WordHtmlConverter.AppendHtml(body, nested, main, settings);
        WordHtmlConverter.AppendHtml(body, several, main, settings);

        await AssertSequence(document, 11);

        // The session shares one bullet definition across the calls, added to the one the
        // document brought. An ordered format takes a definition in each call that uses it.
        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        await Assert.That(numbering.Elements<AbstractNum>().Count()).IsEqualTo(6);
    }

    /// <summary>
    /// A session remembers the next free id for as long as the instances end where it left them.
    /// The cleanup element is last whatever is added ahead of it, so it cannot be what the session
    /// watches: an instance added by other code between two calls has to be seen.
    /// </summary>
    [Test]
    public async Task NumberingSessionSeesAnInstanceAddedAheadOfTheCleanupElement()
    {
        using var stream = Document(pictureBullet: true, definitions: true, cleanup: true);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;
        var body = main.Document!.Body!;
        var settings = new HtmlConvertSettings
        {
            NumberingSession = new()
        };

        WordHtmlConverter.AppendHtml(body, bulleted, main, settings);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        var taken = numbering.Elements<NumberingInstance>().Max(_ => _.NumberID!.Value) + 1;
        numbering.InsertBefore(
            new NumberingInstance(
                new AbstractNumId
                {
                    Val = 0
                })
            {
                NumberID = taken
            },
            numbering.GetFirstChild<NumberingIdMacAtCleanup>());

        WordHtmlConverter.AppendHtml(body, bulleted, main, settings);

        await AssertSequence(document, 4);
    }

    [Test]
    public async Task HeaderAndFooterKeepTheSequence()
    {
        using var stream = Document(pictureBullet: true, definitions: true, cleanup: true);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;

        WordHtmlConverter.SetHeader(main, bulleted);
        WordHtmlConverter.SetFooter(main, ordered);

        await AssertSequence(document, 0);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        var header = main.HeaderParts.Single().Header!;
        var footer = main.FooterParts.Single().Footer!;
        foreach (var item in header.Descendants<Paragraph>().Concat(footer.Descendants<Paragraph>()))
        {
            await AssertResolves(numbering, item);
        }

        await Assert.That(numbering.Elements<NumberingInstance>().Count()).IsEqualTo(3);
    }

    [Test]
    [MatrixDataSource]
    public async Task EnsureListDefinitionsKeepsTheSequence(
        [Matrix(true, false)] bool definitions,
        [Matrix(true, false)] bool cleanup)
    {
        using var stream = Document(pictureBullet: true, definitions, cleanup);
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;

        WordNumbering.EnsureListDefinitions(main);

        await AssertSequence(document, 0);
        await AssertExistingKept(main, pictureBullet: true, definitions, cleanup);
    }

    static async Task AssertSequence(WordprocessingDocument written, int listItems)
    {
        // Validated as a reader gets it, saved and read back.
        using var saved = new MemoryStream();
        using var document = written.Clone(saved, false);
        var main = document.MainDocumentPart!;
        var numbering = main.NumberingDefinitionsPart!.Numbering!;

        var names = numbering.ChildElements.Select(_ => _.LocalName).ToList();
        var inOrder = names.OrderBy(_ => Array.IndexOf(sequence, _));
        await Assert.That(string.Join(" ", names)).IsEqualTo(string.Join(" ", inOrder));

        var errors = new OpenXmlValidator(FileFormatVersions.Microsoft365)
            .Validate(document)
            .Select(_ => $"{_.Description} @ {_.Path?.XPath}")
            .ToList();
        await Assert.That(errors).IsEmpty();

        var abstractIds = numbering.Elements<AbstractNum>().Select(_ => _.AbstractNumberId!.Value).ToList();
        var instanceIds = numbering.Elements<NumberingInstance>().Select(_ => _.NumberID!.Value).ToList();
        await Assert.That(abstractIds).HasDistinctItems();
        await Assert.That(instanceIds).HasDistinctItems();

        var items = main.Document!.Body!
            .Descendants<Paragraph>()
            .Where(_ => _.ParagraphProperties?.NumberingProperties != null)
            .ToList();
        await Assert.That(items.Count).IsEqualTo(listItems);
        foreach (var item in items)
        {
            await AssertResolves(numbering, item);
        }
    }

    // A list paragraph resolves to one instance, and that to one abstract definition.
    static async Task AssertResolves(Numbering numbering, Paragraph item)
    {
        var numId = item.ParagraphProperties!.NumberingProperties!.NumberingId!.Val!.Value;
        var instance = numbering.Elements<NumberingInstance>().Single(_ => _.NumberID?.Value == numId);
        var abstractId = instance.AbstractNumId!.Val!.Value;
        await Assert.That(numbering.Elements<AbstractNum>().Count(_ => _.AbstractNumberId?.Value == abstractId)).IsEqualTo(1);
    }

    // What the document brought is still there, and still first among its kind.
    static async Task AssertExistingKept(MainDocumentPart main, bool pictureBullet, bool definitions, bool cleanup)
    {
        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        await Assert.That(numbering.Elements<NumberingPictureBullet>().Count()).IsEqualTo(Count(pictureBullet));
        await Assert.That(numbering.Elements<NumberingIdMacAtCleanup>().Count()).IsEqualTo(Count(cleanup));
        if (definitions)
        {
            await Assert.That(numbering.Elements<AbstractNum>().First().AbstractNumberId!.Value).IsEqualTo(0);
            var own = numbering.Elements<NumberingInstance>().First();
            await Assert.That(own.NumberID!.Value).IsEqualTo(1);
            await Assert.That(own.AbstractNumId!.Val!.Value).IsEqualTo(0);
        }
    }

    static int Count(bool present)
    {
        if (present)
        {
            return 1;
        }

        return 0;
    }

    static MemoryStream Document(bool pictureBullet, bool definitions, bool cleanup)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new(new Body());

            var numbering = new Numbering();
            if (pictureBullet)
            {
                numbering.Append(
                    new NumberingPictureBullet(new PictureBulletBase())
                    {
                        NumberingPictureBulletId = 0
                    });
            }

            if (definitions)
            {
                numbering.Append(
                    new AbstractNum(
                        new Level(
                            new StartNumberingValue
                            {
                                Val = 1
                            },
                            new DocumentFormat.OpenXml.Wordprocessing.NumberingFormat
                            {
                                Val = NumberFormatValues.UpperLetter
                            },
                            new LevelText
                            {
                                Val = "%1."
                            })
                        {
                            LevelIndex = 0
                        })
                    {
                        AbstractNumberId = 0
                    },
                    new NumberingInstance(
                        new AbstractNumId
                        {
                            Val = 0
                        })
                    {
                        NumberID = 1
                    });
            }

            if (cleanup)
            {
                numbering.Append(
                    new NumberingIdMacAtCleanup
                    {
                        Val = 1
                    });
            }

            main.AddNewPart<NumberingDefinitionsPart>("rExistingNumbering").Numbering = numbering;
        }

        stream.Position = 0;
        return stream;
    }
}
