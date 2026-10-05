using System.Xml.Linq;

static class VerifyOpenXmlConverter
{
    internal static void Initialize()
    {
        VerifierSettings.RegisterFileConverter<SpreadsheetInlineString>(ConvertInlineString);
        VerifierSettings.RegisterFileConverter<SpreadsheetCell>(ConvertCell);
        VerifierSettings.RegisterFileConverter<List<Paragraph>>(ConvertParagraphs);
        VerifierSettings.RegisterFileConverter<List<OpenXmlElement>>(ConvertElements);
        VerifierSettings.RegisterFileConverter<Body>(ConvertBody);
    }

    static ConversionResult ConvertInlineString(SpreadsheetInlineString value, IReadOnlyDictionary<string, object> context) =>
        new(null, "xml", Format(value));

    static ConversionResult ConvertCell(SpreadsheetCell value, IReadOnlyDictionary<string, object> context) =>
        new(null, "xml", Format(value));

    static ConversionResult ConvertParagraphs(List<Paragraph> value, IReadOnlyDictionary<string, object> context) =>
        new(null, "xml", string.Join('\n', value.Select(Format)));

    static ConversionResult ConvertElements(List<OpenXmlElement> value, IReadOnlyDictionary<string, object> context) =>
        new(null, "xml", string.Join('\n', value.Select(Format)));

    static ConversionResult ConvertBody(Body value, IReadOnlyDictionary<string, object> context) =>
        new(null, "xml", Format(value));

    // Indents the element structure only: an element holding text is written on one line,
    // and whitespace under xml:space="preserve" survives the round trip, so the text a
    // snapshot shows is the text the document holds.
    static string Format(OpenXmlElement element) =>
        XElement.Parse(element.OuterXml).ToString();
}
