public class WordListStyleTests
{
    [Test]
    public async Task UnknownListStyleType_OrderedFallsBackToDecimal() =>
        await Assert.That(WordNumberingBuilder.ParseListStyleType("nonsense", null, isOrdered: true)).IsEqualTo(NumberFormatValues.Decimal);

    [Test]
    public async Task UnknownListStyleType_UnorderedFallsBackToBullet() =>
        await Assert.That(WordNumberingBuilder.ParseListStyleType(null, "nonsense", isOrdered: false)).IsEqualTo(NumberFormatValues.Bullet);

    [Test]
    public Task LowerAlphaType()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol type="a">
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
    public Task UpperAlphaType()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol type="A">
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
    public Task LowerRomanType()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol type="i">
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
    public Task UpperRomanType()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol type="I">
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
    public Task CssListStyleType()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol style="list-style-type: lower-roman">
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
    public Task StartAttribute()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol start="5">
              <li>Should be 5</li>
              <li>Should be 6</li>
              <li>Should be 7</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task StartAttributeWithAlpha()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol type="a" start="3">
              <li>Should be c</li>
              <li>Should be d</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task MixedListTypes()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol type="I">
              <li>Roman numeral
                <ol type="a">
                  <li>Alpha nested</li>
                  <li>Alpha nested</li>
                </ol>
              </li>
              <li>Roman numeral</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    // The start override has to name the level the list's paragraphs are at. It was always written
    // for level 0, so the start of a nested list changed a level nothing in that list used.
    [Test]
    public async Task NestedListStartOverridesItsOwnLevel()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();

        var paragraphs = WordHtmlConverter.ToElements(
            """<ol start="3"><li>outer<ol start="5"><li>inner</li></ol></li></ol>""",
            main);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        foreach (var paragraph in paragraphs)
        {
            var properties = paragraph.Descendants<NumberingProperties>().Single();
            var level = properties.NumberingLevelReference!.Val!.Value;
            var instance = numbering.Elements<NumberingInstance>()
                .Single(_ => _.NumberID!.Value == properties.NumberingId!.Val!.Value);
            var levelOverride = instance.Elements<LevelOverride>().Single();

            await Assert.That(levelOverride.LevelIndex!.Value).IsEqualTo(level);
        }

        var starts = numbering.Descendants<StartOverrideNumberingValue>().Select(_ => _.Val!.Value);
        await Assert.That(starts).IsEquivalentTo([3, 5], CollectionOrdering.Matching);
    }

    // Where there is no real numbering the marker is text, and it has to say what the list says:
    // where it starts, which way it counts and in what. Both paths write it from one place, and
    // the spreadsheet rides the segment path, so all three are checked together.
    [Test]
    [Arguments("<ol><li>a</li><li>b</li></ol>", "1. a|2. b")]
    [Arguments("""<ol start="5"><li>a</li><li>b</li></ol>""", "5. a|6. b")]
    [Arguments("<ol reversed><li>a</li><li>b</li><li>c</li></ol>", "3. a|2. b|1. c")]
    [Arguments("""<ol reversed start="10"><li>a</li><li>b</li></ol>""", "10. a|9. b")]
    [Arguments("""<ol type="a"><li>a</li><li>b</li></ol>""", "a) a|b) b")]
    [Arguments("""<ol type="A" start="26"><li>a</li><li>b</li></ol>""", "Z) a|AA) b")]
    [Arguments("""<ol type="i" start="3"><li>a</li><li>b</li></ol>""", "iii) a|iv) b")]
    [Arguments("""<ol type="I" start="9"><li>a</li><li>b</li></ol>""", "IX) a|X) b")]
    [Arguments("""<ol style="list-style-type: lower-roman"><li>a</li><li>b</li></ol>""", "i) a|ii) b")]
    [Arguments("""<ol type="a" start="0"><li>a</li><li>b</li></ol>""", "0. a|a) b")]
    public async Task TextMarkersFollowTheListsAttributes(string html, string expected)
    {
        var elements = string.Join("|", WordHtmlConverter.ToElements(html).Select(_ => _.InnerText));
        var paragraphs = string.Join("|", WordHtmlConverter.ToParagraphs(html).Select(_ => _.InnerText));
        var cell = SpreadsheetHtmlConverter.ToInlineString(html).InnerText.Replace("\n", "|");

        await Assert.That(elements).IsEqualTo(expected);
        await Assert.That(paragraphs).IsEqualTo(expected);
        await Assert.That(cell).IsEqualTo(expected);
    }
}
