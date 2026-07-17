[TestFixture]
public class WordListNumberingTests
{
    [Test]
    public Task UnorderedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ul>
              <li>Alpha</li>
              <li>Beta</li>
              <li>Gamma</li>
            </ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task OrderedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol>
              <li>First</li>
              <li>Second</li>
              <li>Third</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task NestedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <p>Intro paragraph</p>
            <ul>
              <li>Top level
                <ul>
                  <li>Nested item</li>
                  <li>Another nested</li>
                </ul>
              </li>
              <li>Back to top</li>
            </ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task MixedOrderedAndUnordered()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol>
              <li>Numbered
                <ul>
                  <li>Bulleted child</li>
                </ul>
              </li>
              <li>Another numbered</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task SeparateListsRestartNumbering()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol>
              <li>First list item 1</li>
              <li>First list item 2</li>
            </ol>
            <p>Paragraph between lists</p>
            <ol>
              <li>Second list item 1</li>
              <li>Second list item 2</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task DeeplyNestedListCappedAtIlvl8()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ul><li>1
              <ul><li>2
                <ul><li>3
                  <ul><li>4
                    <ul><li>5
                      <ul><li>6
                        <ul><li>7
                          <ul><li>8
                            <ul><li>9
                              <ul><li>10
                                <ul><li>11</li></ul>
                              </li></ul>
                            </li></ul>
                          </li></ul>
                        </li></ul>
                      </li></ul>
                    </li></ul>
                  </li></ul>
                </li></ul>
              </li></ul>
            </li></ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task FallbackWithoutMainDocumentPart()
    {
        var elements = WordHtmlConverter.ToElements(
            """
            <ul>
              <li>Bullet with text prefix</li>
              <li>Another item</li>
            </ul>
            """);
        return Verify(elements);
    }

    // A block-level child makes BuildElement flush before its own children are processed. That
    // flush used to clear the list state BuildListItem had just set, so "x" lost its numPr while
    // the plain <li>y</li> kept its own.
    [Test]
    public Task BlockParagraphInsideListItemKeepsNumbering() =>
        VerifyWithMainPart("<ul><li><p>x</p></li><li>y</li></ul>");

    // The guard that fixes the above must not keep list state alive past the list itself.
    [Test]
    public Task EmptyListItemDoesNotLeakNumberingToNextParagraph() =>
        VerifyWithMainPart("<ul><li></li></ul><p>after</p>");

    static Task VerifyWithMainPart(string html)
    {
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());
        return Verify(WordHtmlConverter.ToElements(html, main));
    }

    [Test]
    public void NumberingPartUsesDeterministicRelationshipId()
    {
        // The numbering part must be added with an explicit, deterministic relationship id — the
        // OpenXML default is random, which makes output non-reproducible.
        using var stream = new MemoryStream();
        using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new(new Body());
        WordHtmlConverter.AppendHtml(main.Document.Body!, "<ul><li>Alpha</li><li>Beta</li></ul>", main);

        var numberingPart = main.NumberingDefinitionsPart;
        Assert.That(numberingPart, Is.Not.Null);
        Assert.That(main.GetIdOfPart(numberingPart!), Is.EqualTo("rNumbering"));
    }

    [Test]
    public void ListDocxIsByteReproducible()
    {
        // Converting the same list HTML twice (into a document with no pre-existing numbering part)
        // must produce byte-identical packages — guards the numbering relationship id determinism.
        var first = RenderListDocx();
        var second = RenderListDocx();
        Assert.That(first, Is.EqualTo(second));
    }

    static byte[] RenderListDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <p>Intro</p>
            <ul>
              <li>Alpha</li>
              <li>Beta</li>
            </ul>
            """,
            stream);
        return stream.ToArray();
    }
}
