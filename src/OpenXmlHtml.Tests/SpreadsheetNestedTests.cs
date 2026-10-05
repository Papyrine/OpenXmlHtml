public class SpreadsheetNestedTests
{
    [Test]
    public Task BoldItalic() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b><i>bold italic</i></b>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <x:r>
                    <x:rPr>
                      <x:b />
                      <x:i />
                    </x:rPr>
                    <x:t xml:space="preserve">bold italic</x:t>
                  </x:r>
                </x:is>
                """);

    [Test]
    public Task BoldUnderlineItalic() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b><u><i>all three</i></u></b>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <x:r>
                    <x:rPr>
                      <x:b />
                      <x:i />
                      <x:u />
                    </x:rPr>
                    <x:t xml:space="preserve">all three</x:t>
                  </x:r>
                </x:is>
                """);

    [Test]
    public Task NestedSameTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b>outer <b>inner</b> outer</b>"));

    [Test]
    public Task PartialOverlap() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b>bold <i>bold-italic</i> bold</b>"));

    [Test]
    public Task DeeplyNested() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<b><i><u><s>all formats</s></u></i></b>"));

    [Test]
    public Task MixedContent() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("start <b>bold <i>both</i></b> <u>under</u> end"));
}
