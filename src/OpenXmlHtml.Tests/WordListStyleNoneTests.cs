[TestFixture]
public class WordListStyleNoneTests
{
    [Test]
    public Task UnorderedListNoMarker()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ul style="list-style-type: none">
              <li>Item one</li>
              <li>Item two</li>
            </ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task OrderedListNoMarker()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol style="list-style-type: none">
              <li>First</li>
              <li>Second</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task NestedListNoMarker()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ul style="list-style-type: none">
              <li>Outer
                <ul style="list-style-type: none">
                  <li>Inner</li>
                </ul>
              </li>
            </ul>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
