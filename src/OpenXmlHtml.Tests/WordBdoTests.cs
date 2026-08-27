[TestFixture]
public class WordBdoTests
{
    [Test]
    public Task BdoRtl() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>before <bdo dir="rtl">reversed</bdo> after</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before </w:t></w:r><w:r><w:rPr><w:rtl /></w:rPr><w:t xml:space=\"preserve\">reversed</w:t></w:r><w:r><w:t xml:space=\"preserve\"> after</w:t></w:r></w:p>");

    [Test]
    public Task BdoLtr() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>before <bdo dir="ltr">normal</bdo> after</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before </w:t></w:r><w:r><w:t xml:space=\"preserve\">normal</w:t></w:r><w:r><w:t xml:space=\"preserve\"> after</w:t></w:r></w:p>");

    [Test]
    public Task BdiText() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>User <bdi>username</bdi> posted</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">User </w:t></w:r><w:r><w:t xml:space=\"preserve\">username</w:t></w:r><w:r><w:t xml:space=\"preserve\"> posted</w:t></w:r></w:p>");
}
