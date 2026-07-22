namespace OpenXmlHtml;

/// <summary>
/// Something the converter recognised in the html and deliberately did not carry into the output.
/// Delivered to <see cref="HtmlConvertSettings.OnDiagnostic"/> as it happens.
/// </summary>
/// <param name="Kind">Whether a css property, an html attribute, or a whole element was discarded.</param>
/// <param name="Name">The css property, attribute, or element name. For example <c>width</c> or <c>iframe</c>.</param>
/// <param name="Value">The discarded value, when the drop turned on one. For example <c>35%</c>.</param>
/// <param name="Reason">Why it could not be expressed.</param>
public readonly record struct HtmlDiagnostic(
    HtmlDiagnosticKind Kind,
    string Name,
    string? Value,
    string Reason);
