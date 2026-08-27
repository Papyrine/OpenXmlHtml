[TestFixture]
public class WordPictureTests
{
    [Test]
    public Task PictureFallsBackToImg() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """
            <picture>
              <source srcset="huge.webp" type="image/webp">
              <img alt="fallback alt text">
            </picture>
            """))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\"> </w:t></w:r><w:r><w:t xml:space=\"preserve\">fallback alt text</w:t></w:r><w:r><w:t xml:space=\"preserve\"> </w:t></w:r></w:p>");

    [Test]
    public Task PictureWithImgAlt() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<picture><source srcset="x.webp"><img alt="cat photo"></picture>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t xml:space=\"preserve\">cat photo</w:t></w:r></w:p>");
}
