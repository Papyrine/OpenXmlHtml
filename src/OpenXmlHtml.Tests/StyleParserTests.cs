[TestFixture]
public class StyleParserTests
{
    [Test]
    public void ParseSingleProperty()
    {
        var result = StyleParser.Parse("color: red");
        Assert.That(result["color"], Is.EqualTo("red"));
    }

    [Test]
    public void ParseMultipleProperties()
    {
        var result = StyleParser.Parse("font-weight: bold; font-style: italic; color: blue");
        Assert.That(result["font-weight"], Is.EqualTo("bold"));
        Assert.That(result["font-style"], Is.EqualTo("italic"));
        Assert.That(result["color"], Is.EqualTo("blue"));
    }

    [Test]
    public void ParseNullStyle()
    {
        var result = StyleParser.Parse(null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseEmptyStyle()
    {
        var result = StyleParser.Parse("");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseTrailingSemicolon()
    {
        var result = StyleParser.Parse("color: red;");
        Assert.That(result["color"], Is.EqualTo("red"));
    }

    [Test]
    public void CaseInsensitiveKeys()
    {
        var result = StyleParser.Parse("Color: red");
        Assert.That(result["color"], Is.EqualTo("red"));
    }

    [Test]
    public void FontSizePt() =>
        Assert.That(StyleParser.ParseFontSize("12pt"), Is.EqualTo(12));

    [Test]
    public void FontSizePx() =>
        Assert.That(StyleParser.ParseFontSize("16px"), Is.EqualTo(12));

    [Test]
    public void FontSizeEm() =>
        Assert.That(StyleParser.ParseFontSize("2em"), Is.EqualTo(24));

    [Test]
    public void FontSizeKeywords()
    {
        Assert.That(StyleParser.ParseFontSize("xx-small"), Is.EqualTo(7));
        Assert.That(StyleParser.ParseFontSize("x-small"), Is.EqualTo(8));
        Assert.That(StyleParser.ParseFontSize("small"), Is.EqualTo(10));
        Assert.That(StyleParser.ParseFontSize("medium"), Is.EqualTo(12));
        Assert.That(StyleParser.ParseFontSize("large"), Is.EqualTo(14));
        Assert.That(StyleParser.ParseFontSize("x-large"), Is.EqualTo(18));
        Assert.That(StyleParser.ParseFontSize("xx-large"), Is.EqualTo(24));
    }

    [Test]
    public void FontSizeRawNumber() =>
        Assert.That(StyleParser.ParseFontSize("14"), Is.EqualTo(14));

    [Test]
    public void FontSizeInvalid() =>
        Assert.That(StyleParser.ParseFontSize("abc"), Is.Null);

    [Test]
    public void MarginShorthandTooManyParts()
    {
        var result = StyleParser.ParseMarginShorthand("10px 20px 30px 40px 50px");
        Assert.That(result.Top, Is.Null);
        Assert.That(result.Right, Is.Null);
        Assert.That(result.Bottom, Is.Null);
        Assert.That(result.Left, Is.Null);
    }

    [Test]
    public void MarginShorthandTabSeparated()
    {
        var result = StyleParser.ParseMarginShorthand("10px\t20px");
        Assert.That(result.Top, Is.EqualTo(150));
        Assert.That(result.Right, Is.EqualTo(300));
        Assert.That(result.Bottom, Is.EqualTo(150));
        Assert.That(result.Left, Is.EqualTo(300));
    }

    [Test]
    public void BorderShorthandTabSeparated()
    {
        var result = StyleParser.ParseBorder("1px\tsolid\tred");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Style, Is.EqualTo(BorderValues.Single));
        Assert.That(result.Color, Is.EqualTo("FF0000"));
        Assert.That(result.SizeEighths, Is.EqualTo(6));
    }
}
