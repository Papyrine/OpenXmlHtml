[TestFixture]
public class WordStyleMappingTests
{
    static (MainDocumentPart MainPart, Body Body) CreateDocumentWithStyles(Stream stream, params (string Id, StyleValues Type)[] styles)
    {
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();

        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        var stylesheet = new Styles();
        foreach (var (id, type) in styles)
        {
            stylesheet.Append(
                new Style
                {
                    StyleId = id,
                    Type = type
                });
        }

        stylesPart.Styles = stylesheet;

        var body = new Body();
        main.Document = new(body);
        return (main, body);
    }

    [Test]
    public Task ParagraphStyleFromClass()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Quote", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<p class="Quote">This should use the Quote style</p>""",
            main);

        return Verify(body);
    }

    [Test]
    public Task CharacterStyleFromClass()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Emphasis", StyleValues.Character));

        WordHtmlConverter.AppendHtml(body,
            """<p>Normal text with <span class="Emphasis">emphasized</span> word</p>""",
            main);

        return Verify(body);
    }

    [Test]
    public Task BothParagraphAndCharacterStyles()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("IntenseQuote", StyleValues.Paragraph),
            ("Strong", StyleValues.Character));

        WordHtmlConverter.AppendHtml(body,
            """<blockquote class="IntenseQuote">Quote with <span class="Strong">strong</span> text</blockquote>""",
            main);

        return Verify(body);
    }

    [Test]
    public Task ClassNotInStyles_NoEffect()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Quote", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<p class="NonExistent">Should render as default</p>""",
            main);

        return Verify(body);
    }

    [Test]
    public Task HeadingStyleTakesPrecedenceOverClass()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("CustomStyle", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<h1 class="CustomStyle">Heading should use Heading1 not CustomStyle</h1>""",
            main);

        return Verify(body);
    }

    [Test]
    public Task CaseInsensitiveStyleLookup()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Quote", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<p class="quote">Case insensitive match</p>""",
            main);

        return Verify(body);
    }

    // A wrapper carrying the target style is the natural way to render an editor fragment into a
    // template's body style, and editor output is always block-level. The class used to be discarded
    // the moment the inner block was entered, leaving these paragraphs unstyled.
    [Test]
    public Task ParagraphStyleFromBlockAncestor()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Body", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<div class="Body"><p>First</p><p>Second</p></div>""",
            main);

        return Verify(body);
    }

    // The ListParagraph fallback is applied only when nothing else set a style, so an ancestor class
    // reaches list items through the same path once it survives the flush.
    [Test]
    public Task ListStyleFromBlockAncestor()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("BodyList", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<ul class="BodyList"><li>First</li><li>Second</li></ul>""",
            main);

        return Verify(body);
    }

    // The style is scoped to the block that set it: what follows must not inherit it.
    [Test]
    public Task BlockAncestorStyleDoesNotLeakToASibling()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Body", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<div class="Body"><p>Inside</p></div><p>After</p>""",
            main);

        return Verify(body);
    }

    // An inner block overrides for its own content, and the outer style resumes afterwards.
    [Test]
    public Task NestedBlockAncestorStylesUnwind()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Outer", StyleValues.Paragraph),
            ("Inner", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<div class="Outer"><p>One</p><div class="Inner"><p>Two</p></div><p>Three</p></div>""",
            main);

        return Verify(body);
    }

    // An element's own class still wins over the one it sits inside.
    [Test]
    public Task OwnClassBeatsBlockAncestor()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Body", StyleValues.Paragraph),
            ("Quote", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<div class="Body"><p class="Quote">Quoted</p><p>Plain</p></div>""",
            main);

        return Verify(body);
    }
}
