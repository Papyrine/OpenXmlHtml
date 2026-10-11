public class WordHeaderFooterTests
{
    [Test]
    public Task SimpleHeader()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body();
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(body, "<p>Document body content</p>", main);
        WordHtmlConverter.SetHeader(main, "<p>Page Header</p>");

        document.Dispose();
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task SimpleFooter()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body();
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(body, "<p>Document body content</p>", main);
        WordHtmlConverter.SetFooter(main, "<p>Page Footer</p>");

        document.Dispose();
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task HeaderAndFooter()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body();
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(body, "<h1>Report</h1><p>Content here.</p>", main);
        WordHtmlConverter.SetHeader(main,
            """<p style="text-align: center"><b>Company Name</b> — Confidential</p>""");
        WordHtmlConverter.SetFooter(main,
            """<p style="text-align: center; font-size: 9pt; color: gray">Page footer — Generated 2024</p>""");

        document.Dispose();
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task FormattedHeader()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body();
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(body, "<p>Body</p>", main);
        WordHtmlConverter.SetHeader(main,
            """
            <table style="width: 500pt; border: none">
              <tr>
                <td style="border: none"><b>ACME Corp</b></td>
                <td style="border: none; text-align: right"><i>Internal Use Only</i></td>
              </tr>
            </table>
            <hr>
            """);

        document.Dispose();
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task FormattedFooter()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        var body = new Body();
        main.Document = new(body);

        WordHtmlConverter.AppendHtml(body, "<p>Body</p>", main);
        WordHtmlConverter.SetFooter(main,
            """
            <hr>
            <p style="font-size: 8pt; color: #666666; text-align: center">
              © 2024 ACME Corp. All rights reserved. | <a href="https://example.com">Privacy Policy</a>
            </p>
            """);

        // Verify body + footer XML (not full docx binary) to avoid
        // non-deterministic relationship IDs in footer .rels
        // (DeterministicIoPackaging 0.24.3+ will fix this)
        var footer = main.FooterParts.First().Footer!;
        return Verify(
            new
            {
                body,
                footer
            });
    }

    const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";

    // An r:embed or r:id is resolved against the part holding the xml. The header's image and link
    // were registered on the main part, so in the header they pointed at nothing.
    [Test]
    public async Task HeaderImageAndLinkBelongToTheHeaderPart()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            main.Document = new(body);
            WordHtmlConverter.AppendHtml(body, "<p>Body</p>", main);
            WordHtmlConverter.SetHeader(main, $"""<p><img src="data:image/png;base64,{png}" width="2" height="2"> <a href="https://example.com">home</a></p>""");
        }

        stream.Position = 0;
        using var reopened = WordprocessingDocument.Open(stream, false);
        var header = reopened.MainDocumentPart!.HeaderParts.Single();

        await Assert.That(header.ImageParts.Count()).IsEqualTo(1);
        await Assert.That(header.HyperlinkRelationships.Count()).IsEqualTo(1);
        await Assert.That(reopened.MainDocumentPart.ImageParts.Count()).IsEqualTo(0);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }

    [Test]
    public async Task FooterImageAndLinkBelongToTheFooterPart()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            main.Document = new(body);
            WordHtmlConverter.AppendHtml(body, "<p>Body</p>", main);
            WordHtmlConverter.SetFooter(main, $"""<p><img src="data:image/png;base64,{png}" width="2" height="2"> <a href="https://example.com">home</a></p>""");
        }

        stream.Position = 0;
        using var reopened = WordprocessingDocument.Open(stream, false);
        var footer = reopened.MainDocumentPart!.FooterParts.Single();

        await Assert.That(footer.ImageParts.Count()).IsEqualTo(1);
        await Assert.That(footer.HyperlinkRelationships.Count()).IsEqualTo(1);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }

    // A header cannot hold a footnote reference, so the title is left out there.
    [Test]
    public async Task HeaderAbbreviationWritesNoFootnote()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            main.Document = new(body);
            WordHtmlConverter.AppendHtml(body, "<p>Body</p>", main);
            WordHtmlConverter.SetHeader(main, """<p><abbr title="World Health Organization">WHO</abbr></p>""");
        }

        stream.Position = 0;
        using var reopened = WordprocessingDocument.Open(stream, false);
        var header = reopened.MainDocumentPart!.HeaderParts.Single().Header!;

        await Assert.That(header.InnerText).IsEqualTo("WHO");
        await Assert.That(header.Descendants<FootnoteReference>().Count()).IsEqualTo(0);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }
}
