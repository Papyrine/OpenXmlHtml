[TestFixture]
public class SpreadsheetListTests
{
    [Test]
    public Task UnorderedList() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ul><li>item one</li><li>item two</li><li>item three</li></ul>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">item one</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">item two</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">item three</x:t></x:r></x:is>
                """);

    [Test]
    public Task OrderedList() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ol><li>first</li><li>second</li><li>third</li></ol>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">1. </x:t></x:r><x:r><x:t xml:space="preserve">first</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">2. </x:t></x:r><x:r><x:t xml:space="preserve">second</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">3. </x:t></x:r><x:r><x:t xml:space="preserve">third</x:t></x:r></x:is>
                """);

    [Test]
    public Task SingleListItem() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ul><li>only item</li></ul>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">● </x:t></x:r><x:r><x:t xml:space=\"preserve\">only item</x:t></x:r></x:is>");

    [Test]
    public Task FormattedListItems() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ul><li><b>bold item</b></li><li><i>italic item</i></li></ul>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">bold item</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:rPr><x:i /></x:rPr><x:t xml:space="preserve">italic item</x:t></x:r></x:is>
                """);

    [Test]
    public Task NestedLists() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ul><li>outer</li><li><ul><li>inner</li></ul></li></ul>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">outer</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">  ○ </x:t></x:r><x:r><x:t xml:space="preserve">inner</x:t></x:r></x:is>
                """);

    [Test]
    public Task DeeplyNestedLists() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ul><li>level 0</li><li><ul><li>level 1</li><li><ul><li>level 2</li></ul></li></ul></li></ul>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">level 0</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">● </x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">  ○ </x:t></x:r><x:r><x:t xml:space="preserve">level 1</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">  ○ </x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">    ■ </x:t></x:r><x:r><x:t xml:space="preserve">level 2</x:t></x:r></x:is>
                """);

    [Test]
    public Task NestedOrderedList() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString("<ol><li>first</li><li><ol><li>nested</li></ol></li></ol>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">1. </x:t></x:r><x:r><x:t xml:space="preserve">first</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">2. </x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">  1. </x:t></x:r><x:r><x:t xml:space="preserve">nested</x:t></x:r></x:is>
                """);
}
