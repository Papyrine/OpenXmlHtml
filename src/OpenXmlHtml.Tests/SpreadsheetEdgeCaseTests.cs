[TestFixture]
public class SpreadsheetEdgeCaseTests
{
    [Test]
    public Task UnclosedTags() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b>bold <i>italic"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold </x:t></x:r><x:r><x:rPr><x:b /><x:i /></x:rPr><x:t xml:space=\"preserve\">italic</x:t></x:r></x:is>");

    [Test]
    public Task ExtraClosingTags() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("text</b></i>more"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">textmore</x:t></x:r></x:is>");

    [Test]
    public Task ConsecutiveBreaks() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("one<br><br><br>two"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">one</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">two</x:t></x:r></x:is>
                """);

    [Test]
    public Task WhitespaceCollapsing() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("  lots   of    spaces  "))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\"> lots of spaces </x:t></x:r></x:is>");

    [Test]
    public Task TabsAndNewlines() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("text\twith\ttabs\nand\nnewlines"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">text with tabs and newlines</x:t></x:r></x:is>");

    [Test]
    public Task SpecialCharacters() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("price: $100 & tax < 10% > 5%"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">price: $100 &amp; tax &lt; 10% &gt; 5%</x:t></x:r></x:is>");

    [Test]
    public Task UnknownTags() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<custom>text</custom>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">text</x:t></x:r></x:is>");

    [Test]
    public Task ImageAlt() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("before <img alt=\"image description\"> after"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">before </x:t></x:r><x:r><x:t xml:space=\"preserve\">image description</x:t></x:r><x:r><x:t xml:space=\"preserve\"> after</x:t></x:r></x:is>");

    [Test]
    public Task SpanWithNoStyle() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<span>plain span</span>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">plain span</x:t></x:r></x:is>");

    [Test]
    public Task MultipleSpaces() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("one     two"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">one two</x:t></x:r></x:is>");

    [Test]
    public Task EmptyTags() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b></b><i></i>text"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">text</x:t></x:r></x:is>");

    [Test]
    public Task MalformedHtml() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b>bold <i>overlap</b> still italic</i>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space=\"preserve\">bold </x:t></x:r><x:r><x:rPr><x:b /><x:i /></x:rPr><x:t xml:space=\"preserve\">overlap</x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\"> still italic</x:t></x:r></x:is>");

    [Test]
    public Task NumericEntity() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("&#169; copyright"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">© copyright</x:t></x:r></x:is>");

    [Test]
    public Task CiteTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<cite>citation</cite>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">citation</x:t></x:r></x:is>");

    [Test]
    public Task DfnTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<dfn>definition</dfn>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">definition</x:t></x:r></x:is>");

    [Test]
    public Task VarTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<var>variable</var>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:i /></x:rPr><x:t xml:space=\"preserve\">variable</x:t></x:r></x:is>");

    [Test]
    public Task SampTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<samp>sample output</samp>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:rPr><x:rFont val=\"Courier New\" /></x:rPr><x:t xml:space=\"preserve\">sample output</x:t></x:r></x:is>");

    [Test]
    public Task InvalidXmlCharsFromEntities() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("before&#1;&#0;&#x1F;after"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">before�after</x:t></x:r></x:is>");

    [Test]
    public Task InvalidXmlCharsRaw() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("before\u0001\u0000\u001fafter"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">beforeafter</x:t></x:r></x:is>");

    [Test]
    public Task LoneSurrogate() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("before\uD800after"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">beforeafter</x:t></x:r></x:is>");
}
