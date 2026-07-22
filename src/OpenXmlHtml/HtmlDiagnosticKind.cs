namespace OpenXmlHtml;

/// <summary>
/// What kind of html a <see cref="HtmlDiagnostic"/> is reporting as discarded.
/// </summary>
public enum HtmlDiagnosticKind
{
    /// <summary>
    /// A css declaration that was read but has no form in the Word output.
    /// </summary>
    DroppedProperty,

    /// <summary>
    /// An html attribute that was read but could not be applied.
    /// </summary>
    IgnoredAttribute,

    /// <summary>
    /// An element that renders in a browser but has no Word equivalent, so it contributed nothing.
    /// </summary>
    UnsupportedElement
}
