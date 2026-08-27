[TestFixture]
public class WordIntegrationTests
{
    [Test]
    public Task AppendHtmlToBody()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordHtmlConverter.AppendHtml(main.Document.Body!,
            "<h1>Report Title</h1><p>This is a <b>bold</b> statement.</p>");

        return Verify(main.Document.Body!)
            .Snapshot("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:pPr><w:pStyle w:val=\"Heading1\" /></w:pPr><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">Report Title</w:t></w:r></w:p><w:p><w:r><w:t xml:space=\"preserve\">This is a </w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">bold</w:t></w:r><w:r><w:t xml:space=\"preserve\"> statement.</w:t></w:r></w:p></w:body>");
    }

    [Test]
    public Task RichFormattedDocument()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            """
            <h1>Status Report</h1>
            <p><b>Date:</b> 2024-01-15</p>
            <p>All systems <span style="color: green"><b>operational</b></span>.</p>
            <ul>
              <li>Server: <span style="color: green">OK</span></li>
              <li>Database: <span style="color: green">OK</span></li>
              <li>Cache: <span style="color: red">Down</span></li>
            </ul>
            """);

        return Verify(paragraphs)
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Status Report</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Date:</w:t></w:r><w:r><w:t xml:space="preserve"> 2024-01-15</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">All systems </w:t></w:r><w:r><w:rPr><w:b /><w:color w:val="008000" /></w:rPr><w:t xml:space="preserve">operational</w:t></w:r><w:r><w:t xml:space="preserve">.</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">Server: </w:t></w:r><w:r><w:rPr><w:color w:val="008000" /></w:rPr><w:t xml:space="preserve">OK</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">Database: </w:t></w:r><w:r><w:rPr><w:color w:val="008000" /></w:rPr><w:t xml:space="preserve">OK</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">● </w:t></w:r><w:r><w:t xml:space="preserve">Cache: </w:t></w:r><w:r><w:rPr><w:color w:val="FF0000" /></w:rPr><w:t xml:space="preserve">Down</w:t></w:r></w:p>
                """);
    }

    [Test]
    public Task FormattedReport()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            """
            <h2>Meeting Notes</h2>
            <p><i>Date: January 15, 2024</i></p>
            <p>Attendees: <b>Alice</b>, <b>Bob</b>, <b>Charlie</b></p>
            <h3>Action Items</h3>
            <ol>
              <li>Review <code>PR #123</code></li>
              <li>Update <u>documentation</u></li>
              <li><del>Fix bug #456</del> <ins>Done!</ins></li>
            </ol>
            """);

        return Verify(paragraphs)
            .Snapshot(
                """
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Meeting Notes</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:i /></w:rPr><w:t xml:space="preserve">Date: January 15, 2024</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">Attendees: </w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Alice</w:t></w:r><w:r><w:t xml:space="preserve">, </w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Bob</w:t></w:r><w:r><w:t xml:space="preserve">, </w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Charlie</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space="preserve">Action Items</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">1. </w:t></w:r><w:r><w:t xml:space="preserve">Review </w:t></w:r><w:r><w:rPr><w:rFonts w:ascii="Courier New" w:hAnsi="Courier New" /></w:rPr><w:t xml:space="preserve">PR #123</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">2. </w:t></w:r><w:r><w:t xml:space="preserve">Update </w:t></w:r><w:r><w:rPr><w:u w:val="single" /></w:rPr><w:t xml:space="preserve">documentation</w:t></w:r></w:p>
                <w:p xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:r><w:t xml:space="preserve">3. </w:t></w:r><w:r><w:rPr><w:strike /></w:rPr><w:t xml:space="preserve">Fix bug #456</w:t></w:r><w:r><w:t xml:space="preserve"> </w:t></w:r><w:r><w:rPr><w:u w:val="single" /></w:rPr><w:t xml:space="preserve">Done!</w:t></w:r></w:p>
                """);
    }

    [Test]
    public Task MultiParagraphWithBreaks()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            "Line 1<br>Line 2<br>Line 3");

        return Verify(paragraphs)
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">Line 1</w:t></w:r><w:r><w:br /></w:r><w:r><w:t xml:space=\"preserve\">Line 2</w:t></w:r><w:r><w:br /></w:r><w:r><w:t xml:space=\"preserve\">Line 3</w:t></w:r></w:p>");
    }
}
