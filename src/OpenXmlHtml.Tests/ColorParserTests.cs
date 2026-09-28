public class ColorParserTests
{
    [Test]
    [Arguments("#FF0000", "FF0000")]
    [Arguments("#ff0000", "FF0000")]
    [Arguments("#F00", "FF0000")]
    [Arguments("#f00", "FF0000")]
    [Arguments("#12345678", "123456")]
    [Arguments("red", "FF0000")]
    [Arguments("Blue", "0000FF")]
    [Arguments("GREEN", "008000")]
    [Arguments("rgb(255, 0, 0)", "FF0000")]
    [Arguments("rgb(0,128,0)", "008000")]
    [Arguments("rgb(0, 0, 255)", "0000FF")]
    [Arguments("rgba(255, 0, 0, 0.5)", "FF0000")]
    [Arguments("rgba(0, 128, 0, 1)", "008000")]
    [Arguments("rgba(0,0,255,0)", "0000FF")]
    [Arguments("rgba(100, 200, 50, 0.8)", "64C832")]
    [Arguments("rgb(100%, 0%, 0%)", "FF0000")]
    [Arguments("rgb(0%, 100%, 0%)", "00FF00")]
    [Arguments("rgba(0%, 0%, 100%, 0.5)", "0000FF")]
    [Arguments("rgb(255.0, 0, 0)", "FF0000")]
    public async Task ValidColors(string input, string expected) =>
        await Assert.That(ColorParser.Parse(input)).IsEqualTo(expected);

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("notacolor")]
    [Arguments("#GGG")]
    [Arguments("rgb(abc)")]
    [Arguments("rgb(255 0 0)")]
    [Arguments("rgb(255, 0)")]
    [Arguments("rgb(abc, 0, 0)")]
    public async Task InvalidColors(string? input) =>
        await Assert.That(ColorParser.Parse(input)).IsNull();

    [Test]
    public async Task NamedColorCoverage()
    {
        await Assert.That(ColorParser.Parse("black")).IsEqualTo("000000");
        await Assert.That(ColorParser.Parse("white")).IsEqualTo("FFFFFF");
        await Assert.That(ColorParser.Parse("yellow")).IsEqualTo("FFFF00");
        await Assert.That(ColorParser.Parse("orange")).IsEqualTo("FFA500");
        await Assert.That(ColorParser.Parse("purple")).IsEqualTo("800080");
        await Assert.That(ColorParser.Parse("pink")).IsEqualTo("FFC0CB");
        await Assert.That(ColorParser.Parse("gray")).IsEqualTo("808080");
        await Assert.That(ColorParser.Parse("grey")).IsEqualTo("808080");
        await Assert.That(ColorParser.Parse("cyan")).IsEqualTo("00FFFF");
        await Assert.That(ColorParser.Parse("magenta")).IsEqualTo("FF00FF");
        await Assert.That(ColorParser.Parse("navy")).IsEqualTo("000080");
        await Assert.That(ColorParser.Parse("teal")).IsEqualTo("008080");
        await Assert.That(ColorParser.Parse("maroon")).IsEqualTo("800000");
        await Assert.That(ColorParser.Parse("olive")).IsEqualTo("808000");
        await Assert.That(ColorParser.Parse("silver")).IsEqualTo("C0C0C0");
        await Assert.That(ColorParser.Parse("crimson")).IsEqualTo("DC143C");
        await Assert.That(ColorParser.Parse("indigo")).IsEqualTo("4B0082");
    }

    [Test]
    public async Task RgbClamping() =>
        await Assert.That(ColorParser.Parse("rgb(300, -10, 128)")).IsEqualTo("FF0080");
}
