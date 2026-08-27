[TestFixture]
public class WordNestedTests
{
    [Test]
    public Task BoldItalic() =>
        Verify(WordHtmlConverter.ToParagraphs("<b><i>bold italic</i></b>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /><w:i /></w:rPr><w:t xml:space=\"preserve\">bold italic</w:t></w:r></w:p>");

    [Test]
    public Task BoldUnderlineItalic() =>
        Verify(WordHtmlConverter.ToParagraphs("<b><u><i>all three</i></u></b>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /><w:i /><w:u w:val=\"single\" /></w:rPr><w:t xml:space=\"preserve\">all three</w:t></w:r></w:p>");

    [Test]
    public Task PartialOverlap() =>
        Verify(WordHtmlConverter.ToParagraphs("<b>bold <i>bold-italic</i> bold</b>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">bold </w:t></w:r><w:r><w:rPr><w:b /><w:i /></w:rPr><w:t xml:space=\"preserve\">bold-italic</w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\"> bold</w:t></w:r></w:p>");

    [Test]
    public Task DeeplyNested() =>
        Verify(WordHtmlConverter.ToParagraphs("<b><i><u><s>all formats</s></u></i></b>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /><w:i /><w:strike /><w:u w:val=\"single\" /></w:rPr><w:t xml:space=\"preserve\">all formats</w:t></w:r></w:p>");

    [Test]
    public Task MixedContent() =>
        Verify(WordHtmlConverter.ToParagraphs("start <b>bold <i>both</i></b> <u>under</u> end"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">start </w:t></w:r><w:r><w:rPr><w:b /></w:rPr><w:t xml:space=\"preserve\">bold </w:t></w:r><w:r><w:rPr><w:b /><w:i /></w:rPr><w:t xml:space=\"preserve\">both</w:t></w:r><w:r><w:t xml:space=\"preserve\"> </w:t></w:r><w:r><w:rPr><w:u w:val=\"single\" /></w:rPr><w:t xml:space=\"preserve\">under</w:t></w:r><w:r><w:t xml:space=\"preserve\"> end</w:t></w:r></w:p>");

    [Test]
    public Task NestedColors() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<span style=\"color: red\">outer <span style=\"color: blue\">inner</span> outer</span>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:color w:val=\"FF0000\" /></w:rPr><w:t xml:space=\"preserve\">outer </w:t></w:r><w:r><w:rPr><w:color w:val=\"0000FF\" /></w:rPr><w:t xml:space=\"preserve\">inner</w:t></w:r><w:r><w:rPr><w:color w:val=\"FF0000\" /></w:rPr><w:t xml:space=\"preserve\"> outer</w:t></w:r></w:p>");
}
