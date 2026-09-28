public class WordFootnoteIdTests
{
    // Footnotes raised inside table cells must get document-unique IDs. Regression: each cell
    // built a fresh context whose footnote counter restarted at zero, producing colliding IDs.
    [Test]
    public async Task FootnotesInTableCellsAreUnique()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td><abbr title="first">A</abbr></td>
                <td><abbr title="second">B</abbr></td>
              </tr>
            </table>
            <p><abbr title="third">C</abbr></p>
            """,
            stream);

        var (footnoteIds, referenceIds) = ReadFootnoteIds(stream);

        await Assert.That(footnoteIds).HasDistinctItems();
        await Assert.That(footnoteIds.Count).IsEqualTo(3);
        await Assert.That(referenceIds).IsEquivalentTo(footnoteIds);
    }

    // A second AppendHtml call against the same document must continue footnote numbering instead
    // of restarting at 1 and colliding with footnotes emitted by the first call.
    [Test]
    public async Task FootnotesAcrossAppendCallsAreUnique()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            WordHtmlConverter.AppendHtml(body, """<p><abbr title="first">A</abbr></p>""", main);
            WordHtmlConverter.AppendHtml(body, """<p><abbr title="second">B</abbr></p>""", main);
            main.Document = new(body);
        }

        var (footnoteIds, _) = ReadFootnoteIds(stream);

        await Assert.That(footnoteIds).HasDistinctItems();
        await Assert.That(footnoteIds.Count).IsEqualTo(2);
    }

    static (List<long> FootnoteIds, List<long> ReferenceIds) ReadFootnoteIds(MemoryStream stream)
    {
        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var main = document.MainDocumentPart!;

        // Real footnotes only — skip the reserved separator (-1) and continuation (0) entries.
        var footnoteIds = main.FootnotesPart!.Footnotes!
            .Elements<Footnote>()
            .Select(_ => _.Id!.Value)
            .Where(_ => _ > 0)
            .ToList();

        var referenceIds = main.Document!.Body!
            .Descendants<FootnoteReference>()
            .Select(_ => _.Id!.Value)
            .ToList();

        return (footnoteIds, referenceIds);
    }
}
