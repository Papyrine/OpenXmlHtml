[TestFixture]
public class WordBookmarkIdTests
{
    // Bookmarks generated from id/name attributes inside table cells must be document-unique.
    // Regression: each cell built a fresh context whose bookmark counter restarted at zero,
    // producing duplicate BookmarkStart IDs (invalid OOXML).
    [Test]
    public void BookmarksInTableCellsAreUnique()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <table>
              <tr>
                <td><p id="a">A</p></td>
                <td><p id="b">B</p></td>
              </tr>
            </table>
            <p id="c">C</p>
            """,
            stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var bookmarkIds = document.MainDocumentPart!.Document!.Body!
            .Descendants<BookmarkStart>()
            .Select(_ => _.Id!.Value)
            .ToList();

        Assert.That(bookmarkIds, Is.Unique);
        Assert.That(bookmarkIds.Count, Is.EqualTo(3));
    }
}
