namespace OpenXmlHtml;

/// <summary>
/// Mutable state shared across multiple <c>WordHtmlConverter.ToElements</c> calls within a
/// single logical render. Passing the same instance to every call ensures that bullet lists
/// across separate HTML fragments reuse one shared abstract numbering definition rather than each
/// allocating their own.
/// </summary>
public class HtmlNumberingSession
{
    /// <summary>
    /// The abstract numbering ID used for bullet lists, populated after the first call that
    /// encounters a <c>&lt;ul&gt;</c>. Can be pre-set to reuse an existing abstract definition.
    /// </summary>
    public int? BulletAbstractNumId { get; set; }

    // The next free numbering id, as left by the last call that used this session, and the element
    // that was last in the numbering part at that moment.
    //
    // Without these every call finds the next id by reading every definition and instance already
    // in the document, so converting N fragments that each hold a list reads 1 + 2 + ... + N of
    // them. The last element is the check that the answer still holds: every instance is appended
    // at the end, so if the numbering part still ends where this session left it, nothing has been
    // added behind its back. If it ends anywhere else, the call counts again.
    internal int NextNumId { get; set; }
    internal OpenXmlElement? LastNumberingElement { get; set; }
}
