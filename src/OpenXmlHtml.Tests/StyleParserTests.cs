public class StyleParserTests
{
    [Test]
    public async Task ParseSingleProperty()
    {
        var result = StyleParser.Parse("color: red");
        await Assert.That(result["color"]).IsEqualTo("red");
    }

    [Test]
    public async Task ParseMultipleProperties()
    {
        var result = StyleParser.Parse("font-weight: bold; font-style: italic; color: blue");
        await Assert.That(result["font-weight"]).IsEqualTo("bold");
        await Assert.That(result["font-style"]).IsEqualTo("italic");
        await Assert.That(result["color"]).IsEqualTo("blue");
    }

    [Test]
    public async Task ParseNullStyle()
    {
        var result = StyleParser.Parse(null);
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task ParseEmptyStyle()
    {
        var result = StyleParser.Parse("");
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task ParseTrailingSemicolon()
    {
        var result = StyleParser.Parse("color: red;");
        await Assert.That(result["color"]).IsEqualTo("red");
    }

    [Test]
    public async Task CaseInsensitiveKeys()
    {
        var result = StyleParser.Parse("Color: red");
        await Assert.That(result["color"]).IsEqualTo("red");
    }

    [Test]
    public async Task FontSizePt() =>
        await Assert.That(StyleParser.ParseFontSize("12pt")).IsEqualTo(12);

    [Test]
    public async Task FontSizePx() =>
        await Assert.That(StyleParser.ParseFontSize("16px")).IsEqualTo(12);

    [Test]
    public async Task FontSizeEm() =>
        await Assert.That(StyleParser.ParseFontSize("2em")).IsEqualTo(24);

    [Test]
    public async Task FontSizeKeywords()
    {
        await Assert.That(StyleParser.ParseFontSize("xx-small")).IsEqualTo(7);
        await Assert.That(StyleParser.ParseFontSize("x-small")).IsEqualTo(8);
        await Assert.That(StyleParser.ParseFontSize("small")).IsEqualTo(10);
        await Assert.That(StyleParser.ParseFontSize("medium")).IsEqualTo(12);
        await Assert.That(StyleParser.ParseFontSize("large")).IsEqualTo(14);
        await Assert.That(StyleParser.ParseFontSize("x-large")).IsEqualTo(18);
        await Assert.That(StyleParser.ParseFontSize("xx-large")).IsEqualTo(24);
    }

    [Test]
    public async Task FontSizeRawNumber() =>
        await Assert.That(StyleParser.ParseFontSize("14")).IsEqualTo(14);

    [Test]
    public async Task FontSizeInvalid() =>
        await Assert.That(StyleParser.ParseFontSize("abc")).IsNull();

    [Test]
    public async Task MarginShorthandTooManyParts()
    {
        var result = StyleParser.ParseMarginShorthand("10px 20px 30px 40px 50px");
        await Assert.That(result.Top).IsNull();
        await Assert.That(result.Right).IsNull();
        await Assert.That(result.Bottom).IsNull();
        await Assert.That(result.Left).IsNull();
    }

    [Test]
    public async Task MarginShorthandTabSeparated()
    {
        var result = StyleParser.ParseMarginShorthand("10px\t20px");
        await Assert.That(result.Top).IsEqualTo(150);
        await Assert.That(result.Right).IsEqualTo(300);
        await Assert.That(result.Bottom).IsEqualTo(150);
        await Assert.That(result.Left).IsEqualTo(300);
    }

    [Test]
    public async Task BorderShorthandTabSeparated()
    {
        var result = StyleParser.ParseBorder("1px\tsolid\tred");
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Style).IsEqualTo(BorderValues.Single);
        await Assert.That(result.Color).IsEqualTo("FF0000");
        await Assert.That(result.SizeEighths).IsEqualTo(6);
    }

    // !important ranks a declaration and says nothing about its value. Left on the value it made
    // the whole declaration unreadable.
    [Test]
    [Arguments("color: red !important", "red")]
    [Arguments("color: red!important", "red")]
    [Arguments("color: red ! IMPORTANT ", "red")]
    [Arguments("color: red", "red")]
    public async Task ParseDropsImportant(string style, string expected) =>
        await Assert.That(StyleParser.Parse(style)["color"]).IsEqualTo(expected);

    [Test]
    public async Task ImportantDeclarationsStillApply()
    {
        var run = WordHtmlConverter.ToElements(
                """<span style="color: red !important; font-weight: bold !important; font-size: 14pt!important">x</span>""")
            .Single()
            .Descendants<WRun>()
            .Single();

        await Assert.That(run.RunProperties!.Color!.Val!.Value).IsEqualTo("FF0000");
        await Assert.That(run.RunProperties.Bold).IsNotNull();
        await Assert.That(run.RunProperties.FontSize!.Val!.Value).IsEqualTo("28");
    }

    // A Word run names one font. The whole fallback list used to be handed over as the name.
    [Test]
    [Arguments("Arial, Helvetica, sans-serif", "Arial")]
    [Arguments("'Times New Roman', serif", "Times New Roman")]
    [Arguments("\"Segoe UI\" , Tahoma", "Segoe UI")]
    [Arguments("Georgia", "Georgia")]
    [Arguments("monospace", "Courier New")]
    [Arguments("sans-serif", null)]
    [Arguments("inherit", null)]
    [Arguments("", null)]
    public async Task ParseFontFamilyTakesTheFirstFamily(string value, string? expected) =>
        await Assert.That(StyleParser.ParseFontFamily(value)).IsEqualTo(expected);

    [Test]
    public async Task FontFamilyStackNamesOneFont()
    {
        var paragraphs = WordHtmlConverter.ToElements(
            """<p style="font-family: 'Times New Roman', serif">a</p><p><font face="Arial, Helvetica">b</font></p>""");

        var fonts = paragraphs
            .SelectMany(_ => _.Descendants<DocumentFormat.OpenXml.Wordprocessing.RunFonts>())
            .Select(_ => _.Ascii!.Value!);

        await Assert.That(fonts).IsEquivalentTo(["Times New Roman", "Arial"], CollectionOrdering.Matching);
    }

    // <font size> is html's 1 to 7 scale, or a signed step from 3. Past 7 it stays a point size.
    [Test]
    [Arguments("1", 7.5)]
    [Arguments("3", 12d)]
    [Arguments("4", 13.5)]
    [Arguments("7", 36d)]
    [Arguments("+1", 13.5)]
    [Arguments("-1", 10d)]
    [Arguments("+9", 36d)]
    [Arguments("-9", 7.5)]
    [Arguments("14", 14d)]
    [Arguments("10.5", 10.5)]
    public async Task ParseFontSizeAttributeReadsTheHtmlScale(string value, double expected) =>
        await Assert.That(StyleParser.ParseFontSizeAttribute(value)).IsEqualTo(expected);

    [Test]
    [Arguments("0")]
    [Arguments("")]
    [Arguments("big")]
    public async Task ParseFontSizeAttributeRejectsWhatIsNotASize(string value) =>
        await Assert.That(StyleParser.ParseFontSizeAttribute(value)).IsNull();

    [Test]
    public async Task FontSizeAttributeFourIsNotFourPoints()
    {
        var paragraph = WordHtmlConverter.ToElements("""<font size="4">text</font>""").Single();

        await Assert.That(paragraph.Descendants<WFontSize>().Single().Val!.Value).IsEqualTo("27");
    }
}
