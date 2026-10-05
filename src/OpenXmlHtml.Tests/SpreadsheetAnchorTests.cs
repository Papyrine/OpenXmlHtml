public class SpreadsheetAnchorTests
{
    [Test]
    public Task SimpleLink() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<a href=\"https://example.com\">Example</a>"));

    [Test]
    public Task LinkWithSameText() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<a href=\"https://example.com\">https://example.com</a>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <x:r>
                    <x:rPr>
                      <x:u />
                      <x:color rgb="FF0563C1" />
                    </x:rPr>
                    <x:t xml:space="preserve">https://example.com</x:t>
                  </x:r>
                </x:is>
                """);

    [Test]
    public Task LinkWithFormatting() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<a href=\"https://example.com\"><b>Bold Link</b></a>"));

    [Test]
    public Task LinkInText() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("Visit <a href=\"https://example.com\">our site</a> for more info."));

    [Test]
    public Task LinkWithNoHref() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<a>anchor text</a>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <x:r>
                    <x:rPr>
                      <x:u />
                      <x:color rgb="FF0563C1" />
                    </x:rPr>
                    <x:t xml:space="preserve">anchor text</x:t>
                  </x:r>
                </x:is>
                """);

    [Test]
    public Task MultipleLinks() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<a href=\"https://one.com\">One</a> and <a href=\"https://two.com\">Two</a>"));
}
