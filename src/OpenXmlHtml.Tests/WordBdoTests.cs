[TestFixture]
public class WordBdoTests
{
    [Test]
    public Task BdoRtl() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>before <bdo dir="rtl">reversed</bdo> after</p>"""));

    [Test]
    public Task BdoLtr() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>before <bdo dir="ltr">normal</bdo> after</p>"""));

    [Test]
    public Task BdiText() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>User <bdi>username</bdi> posted</p>"));
}
