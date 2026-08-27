[TestFixture]
public class WordCheckboxTests
{
    [Test]
    public Task UncheckedCheckbox() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<input type="checkbox"> Task description"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">☐ </w:t></w:r><w:r><w:t xml:space=\"preserve\">Task description</w:t></w:r></w:p>");

    [Test]
    public Task CheckedCheckbox() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<input type="checkbox" checked> Task description"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">☑ </w:t></w:r><w:r><w:t xml:space=\"preserve\">Task description</w:t></w:r></w:p>");

    [Test]
    public Task CheckboxList() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """
            <ul>
              <li><input type="checkbox" checked> Done item</li>
              <li><input type="checkbox"> Pending item</li>
            </ul>
            """))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">☑ </w:t></w:r><w:r><w:t xml:space="preserve">Done item</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">☐ </w:t></w:r><w:r><w:t xml:space="preserve">Pending item</w:t></w:r></w:p>
                """);

    [Test]
    public Task TextInputSkipped() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """before <input type="text" value="ignored"> after"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">before </w:t></w:r><w:r><w:t xml:space=\"preserve\">after</w:t></w:r></w:p>");
}
