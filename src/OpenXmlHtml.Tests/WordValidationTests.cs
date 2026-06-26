using DocumentFormat.OpenXml.Validation;

[TestFixture]
public class WordValidationTests
{
    // The generated docx must satisfy the OOXML schema. This primarily guards the run-property
    // and cell-property ordering: out-of-sequence children are the most common validity failure
    // and are silently repaired by Word but rejected by stricter consumers.
    [Test]
    public void GeneratedDocumentIsSchemaValid()
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

        Assert.That(
            errors,
            Is.Empty,
            () => string.Join("\n", errors.Select(_ => $"{_.Description} ({_.Path?.XPath})")));
    }
}
