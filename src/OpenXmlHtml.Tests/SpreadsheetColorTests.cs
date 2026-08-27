[TestFixture]
public class SpreadsheetColorTests
{
    [Test]
    public Task FontColorAttribute() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<font color=\"#FF0000\">red text</font>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\">red text</x:t></x:r></x:is>");

    [Test]
    public Task FontColorShortHex() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<font color=\"#F00\">red text</font>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\">red text</x:t></x:r></x:is>");

    [Test]
    public Task NamedColor() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<span style=\"color: blue\">blue text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FF0000FF\" /></x:rPr><x:t xml:space=\"preserve\">blue text</x:t></x:r></x:is>");

    [Test]
    public Task RgbColor() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<span style=\"color: rgb(0, 128, 0)\">green text</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FF008000\" /></x:rPr><x:t xml:space=\"preserve\">green text</x:t></x:r></x:is>");

    [Test]
    public Task MultipleColors() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            """
            <span style="color: red">red</span>
            <span style="color: blue">blue</span>
            <span style="color: green">green</span>
            """))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\">red</x:t></x:r><x:r><x:t xml:space=\"preserve\"> </x:t></x:r><x:r><x:rPr><x:color rgb=\"FF0000FF\" /></x:rPr><x:t xml:space=\"preserve\">blue</x:t></x:r><x:r><x:t xml:space=\"preserve\"> </x:t></x:r><x:r><x:rPr><x:color rgb=\"FF008000\" /></x:rPr><x:t xml:space=\"preserve\">green</x:t></x:r></x:is>");

    [Test]
    public Task ColorWithFormatting() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<b style=\"color: #FF0000\">bold red</b>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\">bold red</x:t></x:r></x:is>");

    [Test]
    public Task NestedColors() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"color: red\">outer <span style=\"color: blue\">inner</span> outer</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\">outer </x:t></x:r><x:r><x:rPr><x:color rgb=\"FF0000FF\" /></x:rPr><x:t xml:space=\"preserve\">inner</x:t></x:r><x:r><x:rPr><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\"> outer</x:t></x:r></x:is>");
}
