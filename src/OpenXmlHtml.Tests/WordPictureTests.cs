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
            """));

    [Test]
    public Task PictureWithImgAlt() =>
        Verify(WordHtmlConverter.ToParagraphs(
            """<picture><source srcset="x.webp"><img alt="cat photo"></picture>"""));
}
