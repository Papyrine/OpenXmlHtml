public class WordValidationTests
{
    // The generated docx must satisfy the OOXML schema. This primarily guards the run-property
    // and cell-property ordering: out-of-sequence children are the most common validity failure
    // and are silently repaired by Word but rejected by stricter consumers.
    [Test]
    public async Task GeneratedDocumentIsSchemaValid()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <h1 id="top">Report</h1>
            <p style="font-family:Arial;color:#ff0000;font-size:14pt">
              <b><i><u><s>styled</s></u></i></b> then <sup>super</sup> then
              <span style="letter-spacing:2pt">spaced</span> then
              <span dir="rtl">rtl</span>
            </p>
            <p><abbr title="a footnote">term</abbr></p>
            <table cellpadding="3">
              <tr>
                <td colspan="2" style="border:1px solid #000;background:#eee;vertical-align:top;padding:4px">header</td>
              </tr>
              <tr><td rowspan="2">m</td><td>x</td></tr>
              <tr><td>y</td><td>z</td></tr>
            </table>
            <ol><li>one</li><li>two</li></ol>
            """,
            stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var validator = new OpenXmlValidator();
        var errors = validator.Validate(document).ToList();

        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors.Select(_ => $"{_.Description} ({_.Path?.XPath})")));
    }

    // css reaches the builder in the order it is written, which is not the order CT_PPrBase declares
    // its children: the background is read after the alignment and the border after both, while the
    // schema wants the border first. One paragraph carrying the lot pins the sequence - a snapshot
    // only ever covers the combinations some test happened to write, and none wrote this one.
    [Test]
    public async Task ParagraphPropertiesFollowSchemaOrder()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <p style="margin:10pt 20pt;text-indent:5pt;text-align:center;background:#eeeeee;border:1px solid #000000;writing-mode:vertical-rl;line-height:1.5">styled</p>
            """,
            stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var properties = document.MainDocumentPart!.Document!.Body!
            .Descendants<ParagraphProperties>()
            .First();

        await Assert.That(string.Join(", ", properties.ChildElements.Select(_ => _.LocalName))).IsEqualTo("pBdr, shd, bidi, spacing, ind, jc, textDirection");

        var validator = new OpenXmlValidator();
        var errors = validator.Validate(document).ToList();
        await Assert.That(errors).IsEmpty().Because(string.Join("\n", errors.Select(_ => $"{_.Description} ({_.Path?.XPath})")));
    }

    // A drawing's docPr id has to be unique in the document. Every image carried id 1, which no
    // snapshot could show: none of them holds two images.
    [Test]
    public async Task EachImageHasItsOwnDrawingId()
    {
        const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            $"""
             <p><img src="data:image/png;base64,{png}" width="2" height="2"></p>
             <p><img src="data:image/png;base64,{png}" width="2" height="2" style="float: left">text</p>
             <svg width="10" height="10" xmlns="http://www.w3.org/2000/svg"><circle cx="5" cy="5" r="4"/></svg>
             """,
            stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var ids = document.MainDocumentPart!.Document!
            .Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>()
            .Select(_ => _.Id!.Value)
            .ToList();
        var errors = new OpenXmlValidator().Validate(document)
            .Select(_ => $"{_.Description} ({_.Path?.XPath})")
            .ToList();

        await Assert.That(ids).IsEquivalentTo([1u, 2u, 3u], CollectionOrdering.Matching);
        await Assert.That(errors).IsEmpty().Because(string.Join('\n', errors));
    }

    // tblPr and tcPr each fix the order of their children and allow one of each. Appended as the
    // html supplied them, these came out of order or twice, which Word repairs and a validator
    // does not.
    [Test]
    [Arguments("""<table dir="rtl"><tr><td>x</td></tr></table>""")]
    [Arguments("""<div dir="rtl"><table style="width: 300px; background-color: #eee"><tr><td>x</td></tr></table></div>""")]
    [Arguments("""<table cellpadding="4" style="padding: 4px"><tr><td>x</td></tr></table>""")]
    [Arguments("""<table cellpadding="4" style="background-color: #eee; width: 50%"><tr><td>x</td></tr></table>""")]
    [Arguments("""<table><tr><td bgcolor="#ffffff" style="background-color: #ffffff">x</td></tr></table>""")]
    public async Task TableAndCellPropertiesFollowTheSchema(string html)
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(html, stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var errors = new OpenXmlValidator().Validate(document)
            .Select(_ => $"{_.Description} ({_.Path?.XPath})")
            .ToList();

        await Assert.That(errors).IsEmpty().Because(string.Join('\n', errors));
    }

    // Where css and the attribute it replaced both speak, css is the one kept.
    [Test]
    public async Task CssOutranksTheAttributeWhereBothAreGiven()
    {
        var table = WordHtmlConverter.ToElements(
            """<table cellpadding="10" style="padding: 4px"><tr><td bgcolor="#ff0000" style="background-color: #00ff00">x</td></tr></table>""").Single();

        await Assert.That(table.Descendants<TableCellMarginDefault>().Single().Descendants<TopMargin>().Single().Width!.Value).IsEqualTo("60");
        await Assert.That(table.Descendants<TableCellProperties>().Single().Descendants<DocumentFormat.OpenXml.Wordprocessing.Shading>().Single().Fill!.Value).IsEqualTo("00FF00");
    }

    // A class-styled table writes tblLook, which closes the sequence.
    [Test]
    public async Task StyledTablePropertiesFollowTheSchema()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.AddNewPart<StyleDefinitionsPart>().Styles = new(
            new Style
            {
                Type = StyleValues.Table,
                StyleId = "Grid"
            });

        var table = WordHtmlConverter.ToElements(
            """<table class="Grid" dir="rtl" cellpadding="3" style="background-color: #eee; width: 300px"><tr><td>x</td></tr></table>""",
            main).Single();

        var names = table.GetFirstChild<TableProperties>()!.ChildElements.Select(_ => _.LocalName);

        await Assert.That(names).IsEquivalentTo(["tblStyle", "bidiVisual", "tblW", "shd", "tblLayout", "tblCellMar", "tblLook"], CollectionOrdering.Matching);
    }

    // The same html has to give the same document. A link, a footnote and a header each took a
    // random relationship id, so every run differed.
    [Test]
    public async Task LinksFootnotesAndHeadersAreReproducible()
    {
        var first = RelationshipIds();
        var second = RelationshipIds();

        await Assert.That(first).IsEquivalentTo(second, CollectionOrdering.Matching);
        await Assert.That(first).IsEquivalentTo(["rFooter", "rFootnotes", "rHeader", "rLink", "rLink1"]);
    }

    static List<string> RelationshipIds()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body();
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(
            body,
            """<p><a href="https://example.com/a">a</a> <a href="https://example.com/b">b</a> <abbr title="World Health Organization">WHO</abbr></p>""",
            main);
        WordHtmlConverter.SetHeader(main, "<p>header</p>");
        WordHtmlConverter.SetFooter(main, "<p>footer</p>");

        return main.Parts.Select(_ => _.RelationshipId)
            .Concat(main.HyperlinkRelationships.Select(_ => _.Id))
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
