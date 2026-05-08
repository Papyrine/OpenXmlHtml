[TestFixture]
public class WordFieldsetTests
{
    [Test]
    public Task FieldsetWithLegend()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <fieldset>
              <legend>Personal Info</legend>
              <p>Name: Alice</p>
              <p>Age: 30</p>
            </fieldset>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task FieldsetWithoutLegend()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <fieldset>
              <p>Just some content</p>
            </fieldset>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }

    [Test]
    public Task FieldsetCustomBorder()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <fieldset style="border: 2pt dashed red">
              <legend>Custom border</legend>
              <p>Content</p>
            </fieldset>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
