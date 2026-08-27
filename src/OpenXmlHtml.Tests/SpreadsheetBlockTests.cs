[TestFixture]
public class SpreadsheetBlockTests
{
    [Test]
    public Task Paragraphs() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<p>first paragraph</p><p>second paragraph</p>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">first paragraph</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">second paragraph</x:t></x:r></x:is>
                """);

    [Test]
    public Task Divs() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<div>first div</div><div>second div</div>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">first div</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">second div</x:t></x:r></x:is>
                """);

    [Test]
    public Task Headings() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<h1>heading one</h1><h2>heading two</h2><h3>heading three</h3>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">heading one</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">heading two</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">heading three</x:t></x:r></x:is>
                """);

    [Test]
    public Task MixedBlocksAndInline() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<p>paragraph with <b>bold</b></p><div>div with <i>italic</i></div>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">paragraph with </x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">bold</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">div with </x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space="preserve">italic</x:t></x:r></x:is>
                """);

    [Test]
    public Task Blockquote() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<blockquote>quoted text</blockquote>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">quoted text</x:t></x:r></x:is>");

    [Test]
    public Task PreformattedText() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<pre>  line one\n  line two</pre>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">  line one
                  line two</x:t></x:r></x:is>
                """);

    [Test]
    public Task HorizontalRule() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("above<hr>below"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">above</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">———</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">below</x:t></x:r></x:is>
                """);

    [Test]
    public Task DefinitionList() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<dl><dt>Term</dt><dd>Definition</dd></dl>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Term</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">Definition</x:t></x:r></x:is>
                """);
}
