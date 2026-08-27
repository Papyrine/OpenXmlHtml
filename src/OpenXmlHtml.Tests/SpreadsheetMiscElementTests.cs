[TestFixture]
public class SpreadsheetMiscElementTests
{
    [Test]
    public Task AbbrTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "The <abbr title=\"World Health Organization\">WHO</abbr> recommends it."))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">The </x:t></x:r><x:r><x:t xml:space=\"preserve\">WHO</x:t></x:r><x:r><x:t xml:space=\"preserve\"> recommends it.</x:t></x:r></x:is>");

    [Test]
    public Task AcronymTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "Use <acronym title=\"HyperText Markup Language\">HTML</acronym> for web pages."))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">Use </x:t></x:r><x:r><x:t xml:space=\"preserve\">HTML</x:t></x:r><x:r><x:t xml:space=\"preserve\"> for web pages.</x:t></x:r></x:is>");

    [Test]
    public Task TimeTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "The meeting is at <time datetime=\"14:00\">2 PM</time>."))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">The meeting is at </x:t></x:r><x:r><x:t xml:space=\"preserve\">2 PM</x:t></x:r><x:r><x:t xml:space=\"preserve\">.</x:t></x:r></x:is>");

    [Test]
    public Task QTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "She said <q>hello world</q> to everyone."))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">She said </x:t></x:r><x:r><x:t xml:space=\"preserve\">“</x:t></x:r><x:r><x:t xml:space=\"preserve\">hello world</x:t></x:r><x:r><x:t xml:space=\"preserve\">”</x:t></x:r><x:r><x:t xml:space=\"preserve\"> to everyone.</x:t></x:r></x:is>");

    [Test]
    public Task NestedQ() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<q>outer <q>inner</q> outer</q>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">“</x:t></x:r><x:r><x:t xml:space=\"preserve\">outer </x:t></x:r><x:r><x:t xml:space=\"preserve\">“</x:t></x:r><x:r><x:t xml:space=\"preserve\">inner</x:t></x:r><x:r><x:t xml:space=\"preserve\">”</x:t></x:r><x:r><x:t xml:space=\"preserve\"> outer</x:t></x:r><x:r><x:t xml:space=\"preserve\">”</x:t></x:r></x:is>");

    [Test]
    public Task FigcaptionTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<figure><img alt=\"Chart\"><figcaption>Figure 1: Sales data</figcaption></figure>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:t xml:space="preserve">Chart</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">Figure 1: Sales data</x:t></x:r></x:is>
                """);

    [Test]
    public Task SvgTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "before <svg width=\"100\" height=\"100\"><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg> after"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">before </x:t></x:r><x:r><x:t xml:space=\"preserve\"> after</x:t></x:r></x:is>");

    [Test]
    public Task ArticleTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<article>Article content here</article>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">Article content here</x:t></x:r></x:is>");

    [Test]
    public Task AsideTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<aside>Sidebar content</aside>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">Sidebar content</x:t></x:r></x:is>");

    [Test]
    public Task SectionTag() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<section>Section content</section>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">Section content</x:t></x:r></x:is>");

    [Test]
    public Task DtWithBold() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<dl><dt>Term</dt><dd>Definition of the term</dd></dl>"))
            .Snapshot(
                """
                <x:is xmlns:x="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><x:r><x:rPr><x:b /></x:rPr><x:t xml:space="preserve">Term</x:t></x:r><x:r><x:t xml:space="preserve">
                </x:t></x:r><x:r><x:t xml:space="preserve">Definition of the term</x:t></x:r></x:is>
                """);

    [Test]
    public Task BlockquoteWithQ() =>
        Verify(SpreadsheetHtmlConverter.ToInlineString(
            "<blockquote><q>To be or not to be</q></blockquote>"))
            .Snapshot("<x:is xmlns:x=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><x:r><x:t xml:space=\"preserve\">“</x:t></x:r><x:r><x:t xml:space=\"preserve\">To be or not to be</x:t></x:r><x:r><x:t xml:space=\"preserve\">”</x:t></x:r></x:is>");
}
