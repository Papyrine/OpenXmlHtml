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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Quote\" /></w:pPr><w:r><w:t xml:space=\"preserve\">This should use the Quote style</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t xml:space=\"preserve\">Normal text with </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Emphasis\" /></w:rPr><w:t xml:space=\"preserve\">emphasized</w:t></w:r><w:r><w:t xml:space=\"preserve\"> word</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"IntenseQuote\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Quote with </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Strong\" /></w:rPr><w:t xml:space=\"preserve\">strong</w:t></w:r><w:r><w:t xml:space=\"preserve\"> text</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t xml:space=\"preserve\">Should render as default</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Heading1\" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Heading should use Heading1 not CustomStyle</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"quote\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Case insensitive match</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Body\" /></w:pPr><w:r><w:t xml:space=\"preserve\">First</w:t></w:r></w:p><w:p><w:pPr><w:pStyle w:val=\"Body\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Second</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"BodyList\" /><w:numPr><w:ilvl w:val=\"0\" /><w:numId w:val=\"2\" /></w:numPr><w:contextualSpacing /></w:pPr><w:r><w:t xml:space=\"preserve\">First</w:t></w:r></w:p><w:p><w:pPr><w:pStyle w:val=\"BodyList\" /><w:numPr><w:ilvl w:val=\"0\" /><w:numId w:val=\"2\" /></w:numPr><w:contextualSpacing /></w:pPr><w:r><w:t xml:space=\"preserve\">Second</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Body\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Inside</w:t></w:r></w:p><w:p><w:r><w:t xml:space=\"preserve\">After</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Outer\" /></w:pPr><w:r><w:t xml:space=\"preserve\">One</w:t></w:r></w:p><w:p><w:pPr><w:pStyle w:val=\"Inner\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Two</w:t></w:r></w:p><w:p><w:pPr><w:pStyle w:val=\"Outer\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Three</w:t></w:r></w:p></w:body>");
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

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Quote\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Quoted</w:t></w:r></w:p><w:p><w:pPr><w:pStyle w:val=\"Body\" /></w:pPr><w:r><w:t xml:space=\"preserve\">Plain</w:t></w:r></w:p></w:body>");
    }

    // A table style is the table's to take: the borders, banding and cell margins are the style's
    // once it is named, so the defaults that would otherwise be emitted stand down - direct
    // formatting beats a table style in Word, so leaving them in would override it.
    [Test]
    public Task TableStyleFromClass()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("BrandTable", StyleValues.Table));

        WordHtmlConverter.AppendHtml(body,
            """<table class="BrandTable"><tr><td>Label</td><td>Value</td></tr></table>""",
            main);

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tbl><w:tblPr><w:tblStyle w:val=\"BrandTable\" /><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblLook w:firstRow=\"false\" w:lastRow=\"false\" w:firstColumn=\"false\" w:lastColumn=\"false\" w:noHBand=\"false\" w:noVBand=\"true\" /></w:tblPr><w:tblGrid><w:gridCol /><w:gridCol /></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t xml:space=\"preserve\">Label</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t xml:space=\"preserve\">Value</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:body>");
    }

    // The html says whether the table has a header, and that is what decides the style's firstRow
    // format. Everything else about the look is fixed: html marks no first or last column, and has
    // no way to ask for column banding.
    [Test]
    public Task TableStyleWithHeaderRow()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("BrandTable", StyleValues.Table));

        WordHtmlConverter.AppendHtml(body,
            """<table class="BrandTable"><thead><tr><th>Name</th></tr></thead><tbody><tr><td>Value</td></tr></tbody></table>""",
            main);

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tbl><w:tblPr><w:tblStyle w:val=\"BrandTable\" /><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblLook w:firstRow=\"true\" w:lastRow=\"false\" w:firstColumn=\"false\" w:lastColumn=\"false\" w:noHBand=\"false\" w:noVBand=\"true\" /></w:tblPr><w:tblGrid><w:gridCol /></w:tblGrid><w:tr><w:trPr><w:tblHeader /></w:trPr><w:tc><w:p><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Name</w:t></w:r></w:p></w:tc></w:tr><w:tr><w:tc><w:p><w:r><w:t xml:space=\"preserve\">Value</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:body>");
    }

    // Only a table can take a table style, so one named on anything else has nothing to apply to.
    [Test]
    public Task TableStyleOnAParagraphIsIgnored()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("BrandTable", StyleValues.Table));

        WordHtmlConverter.AppendHtml(body,
            """<p class="BrandTable">Not a table</p>""",
            main);

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t xml:space=\"preserve\">Not a table</w:t></w:r></w:p></w:body>");
    }

    // And a table keeps its default borders when its class names a paragraph style, since nothing
    // took charge of them.
    [Test]
    public Task ParagraphStyleOnATableLeavesTheDefaults()
    {
        using var stream = new MemoryStream();
        var (main, body) = CreateDocumentWithStyles(stream,
            ("Quote", StyleValues.Paragraph));

        WordHtmlConverter.AppendHtml(body,
            """<table class="Quote"><tr><td>Cell</td></tr></table>""",
            main);

        return Verify(body)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tbl><w:tblPr><w:tblW w:w=\"0\" w:type=\"auto\" /><w:tblBorders><w:top w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:left w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:right w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideH w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /><w:insideV w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tblBorders></w:tblPr><w:tblGrid><w:gridCol /></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t xml:space=\"preserve\">Cell</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:body>");
    }
}
