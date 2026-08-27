[TestFixture]
public class SpreadsheetFontTests
{
    [Test]
    public Task FontFaceAttribute() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<font face=\"Arial\">arial text</font>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:rFont val=\"Arial\" /></x:rPr><x:t xml:space=\"preserve\">arial text</x:t></x:r></x:is>");

    [Test]
    public Task FontSizeAttribute() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<font size=\"14\">large text</font>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:sz val=\"14\" /></x:rPr><x:t xml:space=\"preserve\">large text</x:t></x:r></x:is>");

    [Test]
    public Task FontAllAttributes() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<font color=\"#0000FF\" size=\"16\" face=\"Verdana\">styled text</font>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FF0000FF\" /><x:sz val=\"16\" /><x:rFont val=\"Verdana\" /></x:rPr><x:t xml:space=\"preserve\">styled text</x:t></x:r></x:is>");

    [Test]
    public Task InlineStyleFontFamily() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-family: 'Comic Sans MS'\">comic text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:rFont val=\"Comic Sans MS\" /></x:rPr><x:t xml:space=\"preserve\">comic text</x:t></x:r></x:is>");

    [Test]
    public Task InlineStyleFontSizePt() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-size: 18pt\">large text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:sz val=\"18\" /></x:rPr><x:t xml:space=\"preserve\">large text</x:t></x:r></x:is>");

    [Test]
    public Task InlineStyleFontSizePx() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-size: 16px\">pixel sized text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:sz val=\"12\" /></x:rPr><x:t xml:space=\"preserve\">pixel sized text</x:t></x:r></x:is>");

    [Test]
    public Task InlineStyleFontSizeEm() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-size: 1.5em\">em sized text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:sz val=\"18\" /></x:rPr><x:t xml:space=\"preserve\">em sized text</x:t></x:r></x:is>");

    [Test]
    public Task InlineStyleFontSizeKeyword() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-size: x-large\">x-large text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:sz val=\"18\" /></x:rPr><x:t xml:space=\"preserve\">x-large text</x:t></x:r></x:is>");

    [Test]
    public Task SmallTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("normal <small>smaller</small> normal"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">normal </x:t></x:r><x:r><x:rPr><x:sz val=\"9.6\" /></x:rPr><x:t xml:space=\"preserve\">smaller</x:t></x:r><x:r><x:t xml:space=\"preserve\"> normal</x:t></x:r></x:is>");

    [Test]
    public Task CodeTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("normal <code>monospace</code> normal"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">normal </x:t></x:r><x:r><x:rPr><x:rFont val=\"Courier New\" /></x:rPr><x:t xml:space=\"preserve\">monospace</x:t></x:r><x:r><x:t xml:space=\"preserve\"> normal</x:t></x:r></x:is>");

    [Test]
    public Task KbdTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("Press <kbd>Ctrl+C</kbd> to copy"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">Press </x:t></x:r><x:r><x:rPr><x:rFont val=\"Courier New\" /></x:rPr><x:t xml:space=\"preserve\">Ctrl+C</x:t></x:r><x:r><x:t xml:space=\"preserve\"> to copy</x:t></x:r></x:is>");

    [Test]
    public Task Base64ImageSkipped()
    {
        var png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
        return Verify(SpreadsheetHtmlConverter.ToInlineString(
            $"""before <img src="data:image/png;base64,{png}"> after"""))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">before </x:t></x:r><x:r><x:t xml:space=\"preserve\"> after</x:t></x:r></x:is>");
    }

    [Test]
    public Task ImageWithAltTextInSpreadsheet() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            """before <img src="https://example.com/logo.png" alt="Logo"> after"""))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">before </x:t></x:r><x:r><x:t xml:space=\"preserve\">Logo</x:t></x:r><x:r><x:t xml:space=\"preserve\"> after</x:t></x:r></x:is>");
}
