public class WordColorAndFontTests
{
    [Test]
    public Task FontColorAttribute() =>
        Verify(WordHtmlConverter.ToParagraphs("<font color=\"#FF0000\">red text</font>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:color w:val="FF0000" />
                    </w:rPr>
                    <w:t xml:space="preserve">red text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task NamedColor() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"color: blue\">blue text</span>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:color w:val="0000FF" />
                    </w:rPr>
                    <w:t xml:space="preserve">blue text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task RgbColor() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"color: rgb(0, 128, 0)\">green text</span>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:color w:val="008000" />
                    </w:rPr>
                    <w:t xml:space="preserve">green text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task FontFace() =>
        Verify(WordHtmlConverter.ToParagraphs("<font face=\"Arial\">arial text</font>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:rFonts w:ascii="Arial" w:hAnsi="Arial" />
                    </w:rPr>
                    <w:t xml:space="preserve">arial text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task FontSize() =>
        Verify(WordHtmlConverter.ToParagraphs("<font size=\"14\">large text</font>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:sz w:val="28" />
                    </w:rPr>
                    <w:t xml:space="preserve">large text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task InlineStyleFontFamily() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"font-family: Verdana\">verdana</span>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:rFonts w:ascii="Verdana" w:hAnsi="Verdana" />
                    </w:rPr>
                    <w:t xml:space="preserve">verdana</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task InlineStyleFontSizePt() =>
        Verify(WordHtmlConverter.ToParagraphs("<span style=\"font-size: 24pt\">big text</span>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:sz w:val="48" />
                    </w:rPr>
                    <w:t xml:space="preserve">big text</w:t>
                  </w:r>
                </w:p>
                """);

    [Test]
    public Task MultipleStyles() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<span style=\"font-weight: bold; font-style: italic; color: #0000FF; font-size: 16pt\">styled</span>"));

    [Test]
    public Task CodeTag() =>
        Verify(WordHtmlConverter.ToParagraphs("Use <code>Console.WriteLine</code> to print"));

    [Test]
    public Task SmallTag() =>
        Verify(WordHtmlConverter.ToParagraphs("normal <small>smaller</small> normal"));

    [Test]
    public Task ColorWithBold() =>
        Verify(WordHtmlConverter.ToParagraphs("<b style=\"color: red\">bold red</b>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:r>
                    <w:rPr>
                      <w:b />
                      <w:color w:val="FF0000" />
                    </w:rPr>
                    <w:t xml:space="preserve">bold red</w:t>
                  </w:r>
                </w:p>
                """);

    // A tag's default is the browser's own rule for that element, and outranks a value the element
    // would otherwise inherit. Each of these lost its default to an ancestor's declaration.
    [Test]
    public async Task CodeIsMonospaceInsideAFontFamily()
    {
        var runs = Runs("""<p style="font-family: Arial">use <code>foo()</code></p>""");

        await Assert.That(runs[0].RunProperties!.RunFonts!.Ascii!.Value).IsEqualTo("Arial");
        await Assert.That(runs[1].RunProperties!.RunFonts!.Ascii!.Value).IsEqualTo("Courier New");
    }

    [Test]
    public async Task LinkIsBlueInsideColouredText()
    {
        var runs = Runs("""<p style="color: #333333">see <a href="#x">link</a></p>""");

        await Assert.That(runs[0].RunProperties!.Color!.Val!.Value).IsEqualTo("333333");
        await Assert.That(runs[1].RunProperties!.Color!.Val!.Value).IsEqualTo("0563C1");
    }

    [Test]
    public async Task MarkIsYellowInsideABackground()
    {
        var runs = Runs("""<span style="background-color: #eeeeee">some <mark>marked</mark></span>""");

        await Assert.That(runs[0].RunProperties!.Shading!.Fill!.Value).IsEqualTo("EEEEEE");
        await Assert.That(runs[1].RunProperties!.Shading!.Fill!.Value).IsEqualTo("FFFF00");
    }

    // The element's own style is applied after the default, so it still decides.
    [Test]
    public async Task OwnStyleStillOutranksTheTagDefault()
    {
        var runs = Runs("""<a href="#x" style="color: green">link</a> <code style="font-family: Consolas">c</code>""");

        await Assert.That(runs[0].RunProperties!.Color!.Val!.Value).IsEqualTo("008000");
        await Assert.That(runs[^1].RunProperties!.RunFonts!.Ascii!.Value).IsEqualTo("Consolas");
    }

    static List<WRun> Runs(string html) =>
        WordHtmlConverter.ToElements(html)
            .SelectMany(_ => _.Descendants<WRun>())
            .ToList();
}
