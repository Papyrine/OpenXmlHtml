[TestFixture]
public class SpreadsheetBasicTests
{
    [Test]
    public Task PlainText() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("Hello world"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">Hello world</x:t></x:r></x:is>");

    [Test]
    public Task Bold() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b>bold text</b>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold text</x:t></x:r></x:is>");

    [Test]
    public Task Strong() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<strong>strong text</strong>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">strong text</x:t></x:r></x:is>");

    [Test]
    public Task Italic() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<i>italic text</i>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">italic text</x:t></x:r></x:is>");

    [Test]
    public Task Em() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<em>emphasized</em>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">emphasized</x:t></x:r></x:is>");

    [Test]
    public Task Underline() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<u>underlined</u>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:u /></x:rPr><x:t xml:space=\"preserve\">underlined</x:t></x:r></x:is>");

    [Test]
    public Task Strikethrough() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<s>struck</s>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:strike /></x:rPr><x:t xml:space=\"preserve\">struck</x:t></x:r></x:is>");

    [Test]
    public Task StrikeTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<strike>struck</strike>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:strike /></x:rPr><x:t xml:space=\"preserve\">struck</x:t></x:r></x:is>");

    [Test]
    public Task Del() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<del>deleted</del>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:strike /></x:rPr><x:t xml:space=\"preserve\">deleted</x:t></x:r></x:is>");

    [Test]
    public Task Superscript() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("x<sup>2</sup>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">x</x:t></x:r><x:r><x:rPr><x:vertAlign val=\"superscript\" /></x:rPr><x:t xml:space=\"preserve\">2</x:t></x:r></x:is>");

    [Test]
    public Task Subscript() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("H<sub>2</sub>O"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">H</x:t></x:r><x:r><x:rPr><x:vertAlign val=\"subscript\" /></x:rPr><x:t xml:space=\"preserve\">2</x:t></x:r><x:r><x:t xml:space=\"preserve\">O</x:t></x:r></x:is>");

    [Test]
    public Task LineBreak() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("line one<br>line two"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">line one</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">line two</x:t></x:r></x:is>
                """);

    [Test]
    public Task SelfClosingBreak() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("line one<br/>line two"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">line one</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">line two</x:t></x:r></x:is>
                """);

    [Test]
    public Task MixedFormatting() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("normal <b>bold</b> <i>italic</i> normal"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">normal </x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold</x:t></x:r><x:r><x:t xml:space=\"preserve\"> </x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">italic</x:t></x:r><x:r><x:t xml:space=\"preserve\"> normal</x:t></x:r></x:is>");

    [Test]
    public Task EmptyHtml() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(""))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />");

    [Test]
    public Task WhitespaceOnly() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("   "))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\"> </x:t></x:r></x:is>");

    [Test]
    public Task HtmlEntities() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("&amp; &lt; &gt; &quot; &apos;"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">&amp; &lt; &gt; \" '</x:t></x:r></x:is>");

    [Test]
    public Task NonBreakingSpace() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("hello&nbsp;world"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">hello world</x:t></x:r></x:is>");

    [Test]
    public Task InsTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ins>inserted</ins>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:u /></x:rPr><x:t xml:space=\"preserve\">inserted</x:t></x:r></x:is>");
}
