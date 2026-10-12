public class WordListNumberingTests
{
    [Test]
    public Task UnorderedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ul>
              <li>Alpha</li>
              <li>Beta</li>
              <li>Gamma</li>
            </ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task OrderedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol>
              <li>First</li>
              <li>Second</li>
              <li>Third</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task NestedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <p>Intro paragraph</p>
            <ul>
              <li>Top level
                <ul>
                  <li>Nested item</li>
                  <li>Another nested</li>
                </ul>
              </li>
              <li>Back to top</li>
            </ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task MixedOrderedAndUnordered()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol>
              <li>Numbered
                <ul>
                  <li>Bulleted child</li>
                </ul>
              </li>
              <li>Another numbered</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task SeparateListsRestartNumbering()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol>
              <li>First list item 1</li>
              <li>First list item 2</li>
            </ol>
            <p>Paragraph between lists</p>
            <ol>
              <li>Second list item 1</li>
              <li>Second list item 2</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task DeeplyNestedListCappedAtIlvl8()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ul><li>1
              <ul><li>2
                <ul><li>3
                  <ul><li>4
                    <ul><li>5
                      <ul><li>6
                        <ul><li>7
                          <ul><li>8
                            <ul><li>9
                              <ul><li>10
                                <ul><li>11</li></ul>
                              </li></ul>
                            </li></ul>
                          </li></ul>
                        </li></ul>
                      </li></ul>
                    </li></ul>
                  </li></ul>
                </li></ul>
              </li></ul>
            </li></ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task FallbackWithoutMainDocumentPart()
    {
        var elements = WordHtmlConverter.ToElements(
            """
            <ul>
              <li>Bullet with text prefix</li>
              <li>Another item</li>
            </ul>
            """);
        return Verify(elements);
    }

    // A block-level child makes BuildElement flush before its own children are processed. That
    // flush used to clear the list state BuildListItem had just set, so "x" lost its numPr while
    // the plain <li>y</li> kept its own.
    [Test]
    public Task BlockParagraphInsideListItemKeepsNumbering() =>
        VerifyWithMainPart("<ul><li><p>x</p></li><li>y</li></ul>");

    // The guard that fixes the above must not keep list state alive past the list itself.
    [Test]
    public Task EmptyListItemDoesNotLeakNumberingToNextParagraph() =>
        VerifyWithMainPart("<ul><li></li></ul><p>after</p>");

    // A <br> inside an <li> used to flush the paragraph, which reset ListDepth to 0 and so dropped
    // the indentation for everything after it. AddBreakRun does not touch list state now, but the
    // existing coverage ran without a MainDocumentPart and so only exercised depth 0. This nests
    // two levels deep, where a reset would be visible as a lost ilvl.
    [Test]
    public Task BreakInsideNestedListItemKeepsIndentation() =>
        VerifyWithMainPart("<ul><li>outer<ul><li>a<br>b</li><li>c</li></ul></li></ul>");

    static Task VerifyWithMainPart(string html)
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());
        return Verify(WordHtmlConverter.ToElements(html, main));
    }

    [Test]
    public async Task NumberingPartUsesDeterministicRelationshipId()
    {
        // The numbering part must be added with an explicit, deterministic relationship id — the
        // OpenXML default is random, which makes output non-reproducible.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());
        WordHtmlConverter.AppendHtml(main.Document.Body!, "<ul><li>Alpha</li><li>Beta</li></ul>", main);

        var numberingPart = main.NumberingDefinitionsPart;
        await Assert.That(numberingPart).IsNotNull();
        await Assert.That(main.GetIdOfPart(numberingPart!)).IsEqualTo("rNumbering");
    }

    [Test]
    public async Task ListDocxIsReproducible()
    {
        // Converting the same list HTML twice (into a document with no pre-existing numbering part)
        // must produce the same package — guards the numbering relationship id determinism.
        //
        // Compared part by part rather than as raw bytes. A zip entry records when it was written,
        // to a two-second resolution, so two packages built either side of a tick differ in those
        // fields and nowhere else. Comparing the bytes failed whenever the two renders straddled
        // one, which had nothing to do with what this is guarding.
        var first = ReadParts(RenderListDocx());
        var second = ReadParts(RenderListDocx());

        await Assert.That(first.Select(_ => _.Name)).IsEquivalentTo(second.Select(_ => _.Name), CollectionOrdering.Matching);
        await Assert.That(first.Select(_ => _.Content)).IsEquivalentTo(second.Select(_ => _.Content), CollectionOrdering.Matching);
        await Assert.That(first.Select(_ => _.Name)).Contains("/word/numbering.xml as rNumbering");
    }

    // Every part of the package in a stable order: its uri, the relationship id it is reached by,
    // and its content. That is everything a conversion decides; the zip container's own
    // bookkeeping is left out.
    static List<(string Name, string Content)> ReadParts(byte[] docx)
    {
        using var stream = new MemoryStream(docx);
        using var document = WordprocessingDocument.Open(stream, false);
        var parts = new List<(string Name, string Content)>();
        Collect(document, parts);
        return parts.OrderBy(_ => _.Name, StringComparer.Ordinal).ToList();

        static void Collect(OpenXmlPartContainer container, List<(string Name, string Content)> parts)
        {
            foreach (var pair in container.Parts)
            {
                using var partStream = pair.OpenXmlPart.GetStream();
                using var buffer = new MemoryStream();
                partStream.CopyTo(buffer);
                parts.Add(($"{pair.OpenXmlPart.Uri} as {pair.RelationshipId}", Convert.ToBase64String(buffer.ToArray())));
                Collect(pair.OpenXmlPart, parts);
            }
        }
    }

    static byte[] RenderListDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <p>Intro</p>
            <ul>
              <li>Alpha</li>
              <li>Beta</li>
            </ul>
            """,
            stream);
        return stream.ToArray();
    }

    // A session remembers the next free numbering id between calls instead of recounting. The ids
    // it hands out still have to be unique, including after something else has added to the
    // numbering part behind its back.
    [Test]
    public async Task SessionKeepsNumberingIdsUniqueAcrossCalls()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var settings = new HtmlConvertSettings
        {
            NumberingSession = new()
        };

        WordHtmlConverter.ToElements("<ul><li>a</li></ul><ol><li>b</li></ol>", main, settings);
        WordHtmlConverter.ToElements("<ol><li>c</li></ol>", main, settings);

        // Added without the session knowing.
        WordHtmlConverter.ToElements("<ol><li>outside</li></ol><ul><li>outside</li></ul>", main);

        WordHtmlConverter.ToElements("<ul><li>d</li></ul><ol><li>e</li></ol>", main, settings);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        var instanceIds = numbering.Elements<NumberingInstance>().Select(_ => _.NumberID!.Value).ToList();
        var abstractIds = numbering.Elements<AbstractNum>().Select(_ => _.AbstractNumberId!.Value).ToList();

        await Assert.That(instanceIds).HasDistinctItems();
        await Assert.That(abstractIds).HasDistinctItems();
        await Assert.That(instanceIds.Count).IsEqualTo(7);
        await Assert.That(instanceIds.Intersect(abstractIds)).IsEmpty();
    }

    // Ordered lists of one format share an abstract definition instead of each writing its own
    // nine levels. What keeps them separate lists is the start override every instance carries.
    [Test]
    public async Task OrderedListsShareOneDefinitionPerFormat()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();

        var paragraphs = WordHtmlConverter.ToElements(
            """
            <ol><li>a</li><li>b</li></ol>
            <ol><li>c</li></ol>
            <ol type="a"><li>d</li></ol>
            <ol start="4"><li>e<ol><li>f</li></ol></li></ol>
            <table><tr><td><ol><li>g</li></ol></td></tr></table>
            """,
            main);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        var instances = numbering.Elements<NumberingInstance>().ToList();

        // decimal and lower-alpha
        await Assert.That(numbering.Elements<AbstractNum>().Count()).IsEqualTo(2);
        // one instance per list, each with its own start
        await Assert.That(instances.Count).IsEqualTo(6);
        await Assert.That(instances.Select(_ => _.Descendants<StartOverrideNumberingValue>().Single().Val!.Value))
            .IsEquivalentTo([1, 1, 1, 4, 1, 1], CollectionOrdering.Matching);
        // and no two lists use the same instance
        var used = paragraphs
            .SelectMany(_ => _.Descendants<NumberingId>())
            .Select(_ => _.Val!.Value)
            .Distinct()
            .Count();
        await Assert.That(used).IsEqualTo(6);
    }
}
