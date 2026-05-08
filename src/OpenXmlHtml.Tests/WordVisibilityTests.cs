[TestFixture]
public class WordVisibilityTests
{
    [Test]
    public Task DisplayNone() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>visible</p><p style="display: none">hidden</p><p>also visible</p>"""));

    [Test]
    public Task VisibilityHidden() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>visible</p><p style="visibility: hidden">hidden</p><p>also visible</p>"""));

    [Test]
    public Task HiddenAttribute() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>visible</p><p hidden>hidden</p><p>also visible</p>"));

    [Test]
    public Task ScriptTagSkipped() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>before</p><script>alert('hi')</script><p>after</p>"));

    [Test]
    public Task StyleTagSkipped() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>before</p><style>p { color: red }</style><p>after</p>"));

    [Test]
    public Task NoscriptTagSkipped() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>before</p><noscript>fallback</noscript><p>after</p>"));

    [Test]
    public Task DisplayNoneInline() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<p>before <span style="display: none">hidden</span> after</p>"""));
}
