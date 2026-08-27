[TestFixture]
public class SpreadsheetStyleAttributeTests
{
    [Test]
    public Task FontWeightBold() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-weight: bold\">bold</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold</x:t></x:r></x:is>");

    [Test]
    public Task FontWeight700() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-weight: 700\">bold</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold</x:t></x:r></x:is>");

    [Test]
    public Task FontStyleItalic() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-style: italic\">italic</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">italic</x:t></x:r></x:is>");

    [Test]
    public Task TextDecorationUnderline() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"text-decoration: underline\">underlined</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:u /></x:rPr><x:t xml:space=\"preserve\">underlined</x:t></x:r></x:is>");

    [Test]
    public Task TextDecorationLineThrough() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"text-decoration: line-through\">struck</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:strike /></x:rPr><x:t xml:space=\"preserve\">struck</x:t></x:r></x:is>");

    [Test]
    public Task MultipleStyleProperties() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<span style=\"font-weight: bold; font-style: italic; color: #FF0000\">styled</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /><x:i /><x:color rgb=\"FFFF0000\" /></x:rPr><x:t xml:space=\"preserve\">styled</x:t></x:r></x:is>");

    [Test]
    public Task VerticalAlignSuper() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "E = mc<span style=\"vertical-align: super\">2</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">E = mc</x:t></x:r><x:r><x:rPr><x:vertAlign val=\"superscript\" /></x:rPr><x:t xml:space=\"preserve\">2</x:t></x:r></x:is>");

    [Test]
    public Task VerticalAlignSub() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "H<span style=\"vertical-align: sub\">2</span>O"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">H</x:t></x:r><x:r><x:rPr><x:vertAlign val=\"subscript\" /></x:rPr><x:t xml:space=\"preserve\">2</x:t></x:r><x:r><x:t xml:space=\"preserve\">O</x:t></x:r></x:is>");
}
