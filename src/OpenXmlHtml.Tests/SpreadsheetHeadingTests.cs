[TestFixture]
public class SpreadsheetHeadingTests
{
    [Test]
    public Task H1() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h1>Main Title</h1>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Main Title</x:t></x:r></x:is>");

    [Test]
    public Task H2() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h2>Subtitle</h2>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Subtitle</x:t></x:r></x:is>");

    [Test]
    public Task H3() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h3>Section</h3>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Section</x:t></x:r></x:is>");

    [Test]
    public Task H4() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h4>Subsection</h4>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Subsection</x:t></x:r></x:is>");

    [Test]
    public Task H5() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h5>Minor</h5>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Minor</x:t></x:r></x:is>");

    [Test]
    public Task H6() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h6>Smallest</h6>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Smallest</x:t></x:r></x:is>");

    [Test]
    public Task HeadingWithInlineFormatting() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h1>Title with <i>italic</i> word</h1>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">Title with </x:t></x:r><x:r><x:rPr><x:b /><x:i /></x:rPr><x:t xml:space=\"preserve\">italic</x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\"> word</x:t></x:r></x:is>");

    [Test]
    public Task HeadingFollowedByParagraph() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h2>Heading</h2><p>Body text</p>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Heading</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">Body text</x:t></x:r></x:is>
                """);
}
