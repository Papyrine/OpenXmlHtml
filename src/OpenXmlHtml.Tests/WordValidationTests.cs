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

    // A css length is unbounded and may be negative. Written through as it stood, each of these
    // produced an attribute the schema rejects.
    [Test]
    [Arguments("""<p style="margin-top: -10px; margin-bottom: -4px">x</p>""")]
    [Arguments("""<p style="margin: -1in 0">x</p>""")]
    [Arguments("""<p style="line-height: -2">x</p>""")]
    [Arguments("""<p style="line-height: -12pt">x</p>""")]
    [Arguments("""<span style="font-size: -5px">x</span>""")]
    [Arguments("""<span style="font-size: 0">x</span>""")]
    [Arguments("""<span style="font-size: 5000pt">x</span>""")]
    [Arguments("""<span style="letter-spacing: 5000px">x</span>""")]
    [Arguments("""<span style="letter-spacing: -5000px">x</span>""")]
    [Arguments("""<span style="word-spacing: 5000px">x</span>""")]
    [Arguments("""<table><tr style="height: -5px"><td>x</td></tr></table>""")]
    [Arguments("""<table><tr height="99999"><td>x</td></tr></table>""")]
    [Arguments("""<img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==" width="-5" height="5">""")]
    [Arguments("""<img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==" style="width: -5px; height: -5px">""")]
    [Arguments("""<style>@page { margin: -1in; size: -5in -5in }</style><p>x</p>""")]
    public async Task OutOfRangeLengthsStayWithinTheSchema(string html)
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

    // A negative margin has no form in Word. The nearest it can say is none.
    [Test]
    public async Task NegativeVerticalMarginBecomesNone()
    {
        var paragraph = WordHtmlConverter.ToElements("""<p style="margin-top: -10px">x</p>""").Single();

        await Assert.That(paragraph.Descendants<SpacingBetweenLines>().Single().Before!.Value).IsEqualTo("0");
    }
}
