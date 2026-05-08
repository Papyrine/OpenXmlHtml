[TestFixture]
public class WordCheckboxTests
{
    [Test]
    public Task UncheckedCheckbox() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<input type="checkbox"> Task description"""));

    [Test]
    public Task CheckedCheckbox() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<input type="checkbox" checked> Task description"""));

    [Test]
    public Task CheckboxList() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """
            <ul>
              <li><input type="checkbox" checked> Done item</li>
              <li><input type="checkbox"> Pending item</li>
            </ul>
            """));

    [Test]
    public Task TextInputSkipped() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """before <input type="text" value="ignored"> after"""));
}
