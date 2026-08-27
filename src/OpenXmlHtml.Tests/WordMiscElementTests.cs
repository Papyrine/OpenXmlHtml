[TestFixture]
public class WordMiscElementTests
{
    [Test]
    public Task AbbrTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "The <abbr title=\"World Health Organization\">WHO</abbr> recommends it."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">The </w:t></w:r><w:r><w:t xml:space=\"preserve\">WHO</w:t></w:r><w:r><w:t xml:space=\"preserve\"> recommends it.</w:t></w:r></w:p>");

    [Test]
    public Task AcronymTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "Use <acronym title=\"HyperText Markup Language\">HTML</acronym> for web pages."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Use </w:t></w:r><w:r><w:t xml:space=\"preserve\">HTML</w:t></w:r><w:r><w:t xml:space=\"preserve\"> for web pages.</w:t></w:r></w:p>");

    [Test]
    public Task TimeTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "The meeting is at <time datetime=\"14:00\">2 PM</time>."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">The meeting is at </w:t></w:r><w:r><w:t xml:space=\"preserve\">2 PM</w:t></w:r><w:r><w:t xml:space=\"preserve\">.</w:t></w:r></w:p>");

    [Test]
    public Task QTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "She said <q>hello world</q> to everyone."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">She said </w:t></w:r><w:r><w:t xml:space=\"preserve\">“</w:t></w:r><w:r><w:t xml:space=\"preserve\">hello world</w:t></w:r><w:r><w:t xml:space=\"preserve\">”</w:t></w:r><w:r><w:t xml:space=\"preserve\"> to everyone.</w:t></w:r></w:p>");

    [Test]
    public Task FigcaptionTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<figure><img alt=\"Chart\"><figcaption>Figure 1: Sales data</figcaption></figure>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Chart</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Figure 1: Sales data</w:t></w:r></w:p>
                """);

    [Test]
    public Task SvgTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "before <svg width=\"100\" height=\"100\"><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg> after"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before </w:t></w:r><w:r><w:t xml:space=\"preserve\"> after</w:t></w:r></w:p>");

    [Test]
    public Task ArticleTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<article>Article content here</article>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Article content here</w:t></w:r></w:p>");

    [Test]
    public Task SectionTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<section>Section content</section>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Section content</w:t></w:r></w:p>");

    [Test]
    public Task DtWithBold() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<dl><dt>Term</dt><dd>Definition of the term</dd></dl>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Term</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Definition of the term</w:t></w:r></w:p>
                """);

    [Test]
    public Task BlockquoteWithQ() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<blockquote><q>To be or not to be</q></blockquote>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">“</w:t></w:r><w:r><w:t xml:space=\"preserve\">To be or not to be</w:t></w:r><w:r><w:t xml:space=\"preserve\">”</w:t></w:r></w:p>");

    [Test]
    public Task NavTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<nav>Navigation content</nav>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Navigation content</w:t></w:r></w:p>");

    [Test]
    public Task MainTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<main>Main content</main>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Main content</w:t></w:r></w:p>");

    [Test]
    public Task HeaderTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<header>Header content</header>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Header content</w:t></w:r></w:p>");

    [Test]
    public Task FooterTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<footer>Footer content</footer>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Footer content</w:t></w:r></w:p>");

    [Test]
    public Task AsideTag() =>
        Verify(WordHtmlConverter.ToParagraphs("<aside>Sidebar content</aside>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Sidebar content</w:t></w:r></w:p>");

    [Test]
    public Task DfnTag() =>
        Verify(WordHtmlConverter.ToParagraphs("A <dfn>variable</dfn> stores data."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">A </w:t></w:r><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\">variable</w:t></w:r><w:r><w:t xml:space=\"preserve\"> stores data.</w:t></w:r></w:p>");

    [Test]
    public Task CiteTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "From <cite>The Great Gatsby</cite> by F. Scott Fitzgerald."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">From </w:t></w:r><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\">The Great Gatsby</w:t></w:r><w:r><w:t xml:space=\"preserve\"> by F. Scott Fitzgerald.</w:t></w:r></w:p>");

    [Test]
    public Task VarTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "The variable <var>x</var> represents the unknown."))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">The variable </w:t></w:r><w:r><w:rPr><w:i /></w:rPr><w:t xml:space=\"preserve\">x</w:t></w:r><w:r><w:t xml:space=\"preserve\"> represents the unknown.</w:t></w:r></w:p>");

    [Test]
    public Task DetailsAndSummaryTags() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<details><summary>Click to expand</summary>Hidden content here</details>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Click to expand</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Hidden content here</w:t></w:r></w:p>
                """);

    [Test]
    public Task AddressTag() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<address>123 Main St, Anytown USA</address>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">123 Main St, Anytown USA</w:t></w:r></w:p>");
}
