[TestFixture]
public class WordHeadingTests
{
    [Test]
    public Task H1() =>
        Verify(WordHtmlConverter.ToParagraphs("<h1>Main Title</h1>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Main Title</w:t></w:r></w:p>");

    [Test]
    public Task H2() =>
        Verify(WordHtmlConverter.ToParagraphs("<h2>Subtitle</h2>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Subtitle</w:t></w:r></w:p>");

    [Test]
    public Task H3() =>
        Verify(WordHtmlConverter.ToParagraphs("<h3>Section</h3>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Section</w:t></w:r></w:p>");

    [Test]
    public Task HeadingWithInlineFormatting() =>
        Verify(WordHtmlConverter.ToParagraphs("<h1>Title with <i>italic</i> word</h1>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Title with </w:t></w:r><w:r><w:rPr><w:b /><w:i /></w:rPr><w:t xml:space=\"preserve\">italic</w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\"> word</w:t></w:r></w:p>");

    [Test]
    public Task HeadingFollowedByParagraph() =>
        Verify(WordHtmlConverter.ToParagraphs("<h2>Heading</h2><p>Body text</p>"))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Body text</w:t></w:r></w:p>
                """);

    [Test]
    public Task HeadingStyles() =>
        Verify(WordHtmlConverter.ToElements(
            """
            <h1>Heading 1</h1>
            <h2>Heading 2</h2>
            <h3>Heading 3</h3>
            <h4>Heading 4</h4>
            <h5>Heading 5</h5>
            <h6>Heading 6</h6>
            <p>Normal paragraph</p>
            """))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading1" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 1</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading2" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 2</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading3" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 3</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading4" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 4</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading5" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 5</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading6" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 6</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Normal paragraph</w:t></w:r></w:p>
                """);

    [Test]
    public Task HeadingOffsetShifts() =>
        Verify(
            WordHtmlConverter.ToElements(
                """
                <h1>Heading 1</h1>
                <h2>Heading 2</h2>
                <h3>Heading 3</h3>
                """,
                main: null,
                new()
                {
                    HeadingLevelOffset = 1
                }))
                .Snapshot(
                    """
                    <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading2" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 1</w:t></w:r></w:p>
                    <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading3" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 2</w:t></w:r></w:p>
                    <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading4" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Heading 3</w:t></w:r></w:p>
                    """);

    [Test]
    public Task HeadingOffsetClampsAtNine() =>
        Verify(WordHtmlConverter.ToElements(
            "<h5>Deep</h5><h6>Deeper</h6>",
            main: null,
            new()
            {
                HeadingLevelOffset = 5
            }))
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading9" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Deep</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:pPr><w:pStyle w:val="Heading9" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Deeper</w:t></w:r></w:p>
                """);

    [Test]
    public Task HeadingStylesDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <h1>Chapter One</h1>
            <p>Introduction text.</p>
            <h2>Section 1.1</h2>
            <p>Details here.</p>
            <h2>Section 1.2</h2>
            <p>More details.</p>
            """,
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
