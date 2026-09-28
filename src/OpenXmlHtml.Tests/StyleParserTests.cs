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
}
