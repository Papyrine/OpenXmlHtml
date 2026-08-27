[TestFixture]
public class WordReversedListTests
{
    [Test]
    public Task ReversedOrderedList()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol reversed>
              <li>Third (should be 3)</li>
              <li>Second (should be 2)</li>
              <li>First (should be 1)</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task ReversedWithStart()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <ol reversed start="10">
              <li>Should be 10</li>
              <li>Should be 9</li>
              <li>Should be 8</li>
            </ol>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task ReversedFallbackWithoutMainPart()
    {
        var elements = WordHtmlConverter.ToElements(
            """
            <ol reversed>
              <li>Third</li>
              <li>Second</li>
              <li>First</li>
            </ol>
            """);
        return Verify(elements)
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">3. </w:t></w:r><w:r><w:t xml:space="preserve">Third</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">2. </w:t></w:r><w:r><w:t xml:space="preserve">Second</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">1. </w:t></w:r><w:r><w:t xml:space="preserve">First</w:t></w:r></w:p>
                """);
    }
}
