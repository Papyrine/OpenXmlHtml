[TestFixture]
public class WordRubyTests
{
    [Test]
    public Task BasicRuby() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<ruby>漢<rt>kan</rt></ruby><ruby>字<rt>ji</rt></ruby>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">漢</w:t></w:r><w:r><w:t xml:space=\"preserve\">(</w:t></w:r><w:r><w:rPr><w:sz w:val=\"14\" /></w:rPr><w:t xml:space=\"preserve\">kan</w:t></w:r><w:r><w:t xml:space=\"preserve\">)</w:t></w:r><w:r><w:t xml:space=\"preserve\">字</w:t></w:r><w:r><w:t xml:space=\"preserve\">(</w:t></w:r><w:r><w:rPr><w:sz w:val=\"14\" /></w:rPr><w:t xml:space=\"preserve\">ji</w:t></w:r><w:r><w:t xml:space=\"preserve\">)</w:t></w:r></w:p>");

    [Test]
    public Task RubyWithRpFallback() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<ruby>漢<rp>(</rp><rt>kan</rt><rp>)</rp></ruby>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">漢</w:t></w:r><w:r><w:t xml:space=\"preserve\">(</w:t></w:r><w:r><w:rPr><w:sz w:val=\"14\" /></w:rPr><w:t xml:space=\"preserve\">kan</w:t></w:r><w:r><w:t xml:space=\"preserve\">)</w:t></w:r></w:p>");

    [Test]
    public Task RubyInParagraph() =>
        Verify(WordHtmlConverter.ToParagraphs(
            "<p>The <ruby>kanji<rt>annotation</rt></ruby> word.</p>"))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">The </w:t></w:r><w:r><w:t xml:space=\"preserve\">kanji</w:t></w:r><w:r><w:t xml:space=\"preserve\">(</w:t></w:r><w:r><w:rPr><w:sz w:val=\"14\" /></w:rPr><w:t xml:space=\"preserve\">annotation</w:t></w:r><w:r><w:t xml:space=\"preserve\">)</w:t></w:r><w:r><w:t xml:space=\"preserve\"> word.</w:t></w:r></w:p>");
}
