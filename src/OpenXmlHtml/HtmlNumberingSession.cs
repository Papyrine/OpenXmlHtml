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
    // them. The last element is the check that the answer still holds: every instance is added
    // after the ones already there, so if the instances still end where this session left them,
    // nothing has been added behind its back. If they end anywhere else, the call counts again.
    // The w:numIdMacAtCleanup that closes some numbering parts is not that element: an instance
    // goes in ahead of it, so it is last whatever has been added.
    internal int NextNumId { get; set; }
    internal OpenXmlElement? LastNumberingElement { get; set; }

    // The highest bookmark id handed out through this session. The elements a call returns are not
    // in the document until the caller puts them there, so their bookmarks cannot be found by
    // looking; this is how the next call knows about them.
    internal int LastBookmarkId { get; set; }
}
