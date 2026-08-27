[TestFixture]
public class WordImageFloatTests
{
    const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";

    static List<OpenXmlElement> Build(string html)
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        return WordHtmlConverter.ToElements(html, main);
    }

    [Test]
    public Task FloatLeft() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="float: left; width: 100px; height: 100px"> wrapped text</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:drawing><wp:anchor distT=\"0\" distB=\"0\" distL=\"114300\" distR=\"114300\" simplePos=\"0\" relativeHeight=\"0\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"><wp:simplePos x=\"0\" y=\"0\" /><wp:positionH relativeFrom=\"margin\"><wp:align>left</wp:align></wp:positionH><wp:positionV relativeFrom=\"paragraph\"><wp:posOffset>0</wp:posOffset></wp:positionV><wp:extent cx=\"952500\" cy=\"952500\" /><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\" /><wp:wrapSquare wrapText=\"bothSides\" /><wp:docPr id=\"1\" name=\"Image\" /><wp:cNvGraphicFramePr /><a:graphic xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Image\" /><pic:cNvPicPr /></pic:nvPicPr><pic:blipFill><a:blip r:embed=\"rImage1\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" /><a:stretch><a:fillRect /></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\" /><a:ext cx=\"952500\" cy=\"952500\" /></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst /></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r><w:r><w:t xml:space=\"preserve\"> wrapped text</w:t></w:r></w:p>");

    [Test]
    public Task FloatRight() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="float: right; width: 100px; height: 100px"> wrapped text</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:drawing><wp:anchor distT=\"0\" distB=\"0\" distL=\"114300\" distR=\"114300\" simplePos=\"0\" relativeHeight=\"0\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"><wp:simplePos x=\"0\" y=\"0\" /><wp:positionH relativeFrom=\"margin\"><wp:align>right</wp:align></wp:positionH><wp:positionV relativeFrom=\"paragraph\"><wp:posOffset>0</wp:posOffset></wp:positionV><wp:extent cx=\"952500\" cy=\"952500\" /><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\" /><wp:wrapSquare wrapText=\"bothSides\" /><wp:docPr id=\"1\" name=\"Image\" /><wp:cNvGraphicFramePr /><a:graphic xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Image\" /><pic:cNvPicPr /></pic:nvPicPr><pic:blipFill><a:blip r:embed=\"rImage1\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" /><a:stretch><a:fillRect /></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\" /><a:ext cx=\"952500\" cy=\"952500\" /></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst /></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r><w:r><w:t xml:space=\"preserve\"> wrapped text</w:t></w:r></w:p>");

    [Test]
    public Task FloatNone() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="float: none; width: 100px"> inline</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"><wp:extent cx=\"952500\" cy=\"952500\" /><wp:docPr id=\"1\" name=\"Image\" /><a:graphic xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Image\" /><pic:cNvPicPr /></pic:nvPicPr><pic:blipFill><a:blip r:embed=\"rImage1\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" /><a:stretch><a:fillRect /></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\" /><a:ext cx=\"952500\" cy=\"952500\" /></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst /></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r><w:r><w:t xml:space=\"preserve\"> inline</w:t></w:r></w:p>");

    [Test]
    public Task FloatLeftSvg() =>
        Verify(Build("""<p><svg style="float: left" width="50" height="50"><rect width="50" height="50"/></svg> next to svg</p>"""))
            .Snapshot("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:drawing><wp:anchor distT=\"0\" distB=\"0\" distL=\"114300\" distR=\"114300\" simplePos=\"0\" relativeHeight=\"0\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\" xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\"><wp:simplePos x=\"0\" y=\"0\" /><wp:positionH relativeFrom=\"margin\"><wp:align>left</wp:align></wp:positionH><wp:positionV relativeFrom=\"paragraph\"><wp:posOffset>0</wp:posOffset></wp:positionV><wp:extent cx=\"476250\" cy=\"476250\" /><wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\" /><wp:wrapSquare wrapText=\"bothSides\" /><wp:docPr id=\"1\" name=\"Image\" /><wp:cNvGraphicFramePr /><a:graphic xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic xmlns:pic=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:nvPicPr><pic:cNvPr id=\"0\" name=\"Image\" /><pic:cNvPicPr /></pic:nvPicPr><pic:blipFill><a:blip r:embed=\"rImage1\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><a:extLst><a:ext uri=\"{96DAC541-7B7A-43D3-8B79-37D633B846F1}\"><asvg:svgBlip r:embed=\"rImage1s\" xmlns:asvg=\"http://schemas.microsoft.com/office/drawing/2016/SVG/main\" /></a:ext></a:extLst></a:blip><a:stretch><a:fillRect /></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\" /><a:ext cx=\"476250\" cy=\"476250\" /></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst /></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r><w:r><w:t xml:space=\"preserve\"> next to svg</w:t></w:r></w:p>");

    [Test]
    public Task FloatLeftDocx()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            $"""<p><img src="data:image/png;base64,{png}" style="float: left; width: 80px; height: 80px"> body text after image</p>""",
            stream);
        stream.Position = 0;
        return Verify(stream, "docx");
    }
}
