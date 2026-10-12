class WordBuildContext
{
    internal List<OpenXmlElement> CurrentRuns = [];
    internal int ImageIndex;

    // The image parts this conversion has already written, by content, so that an image used more
    // than once is stored once. Shared with table cells by reference.
    internal Dictionary<string, StoredImage> StoredImages = [];
    internal int ListDepth;
    internal int HeadingLevel;
    internal int FootnoteIndex;
    internal int BookmarkId;

    // Whether BookmarkId has been moved past the bookmark ids already in use. That means reading
    // every bookmark in the document, so it waits until the html actually asks for one: most
    // fragments carry no id at all.
    internal bool BookmarkIdSeeded;

    // Bookmark markers waiting for a paragraph to sit in. A block's bookmark opens before its
    // content, but a block that holds other blocks has no paragraph of its own: written straight
    // into CurrentRuns the marker was flushed out as a paragraph by itself when the first child
    // block arrived, and the closing one the same way on the way out. Held here instead, they go
    // at the head of the next paragraph that is actually built. Shared with table cells, so a
    // bookmark opened just ahead of a table lands in its first cell.
    internal List<OpenXmlElement> PendingBookmarks = [];
    internal MainDocumentPart? MainPart;

    // The part the built content will live in, which is where its image parts and hyperlink
    // relationships have to be registered: an r:embed or r:id is resolved against the part holding
    // the xml, not against the document. The main part for a body, the header or footer part for
    // those. Styles, numbering and footnotes stay document-level and keep using MainPart.
    internal OpenXmlPart? RelationshipPart;
    internal HtmlConvertSettings? Settings;
    internal Dictionary<string, StyleType>? StyleMap;
    internal string? ParagraphStyleId;

    // The paragraph style owed by an enclosing block, so it survives into the paragraphs inside it.
    //
    // A class maps to a paragraph style on the element carrying it, but entering a block child
    // flushes first, and that flush would otherwise clear the pending style before any text arrived:
    // <div class="Body"><p>x</p></div> lost Body entirely, and the class only worked for inline
    // content sitting straight inside the element. Editor output is always block-level, so it failed
    // exactly where it was most wanted. A flush now falls back to this rather than to null, and
    // BuildElement saves and restores it around each block that sets one, so siblings and nesting
    // behave. Same shape as ListItemDepth below, and there for the same reason.
    internal string? AmbientParagraphStyleId;

    internal ParagraphFormatState? ParagraphFormat;

    // The paragraph formatting owed by an enclosing block, for the same reason and with the same
    // shape as AmbientParagraphStyleId: <div style="text-align:center"><p>x</p></div> lost the
    // alignment to the flush on entering the <p>.
    internal ParagraphFormatState? AmbientParagraphFormat;
    internal bool ParagraphRightToLeft;
    internal Stack<(int NumId, int Ilvl, bool IsOrdered, bool Inside, bool NoMarker)> ListStack = new();
    internal int? BulletAbstractNumId;

    // The abstract definition already written for each ordered number format. Every <ol> used to
    // get one of its own, nine levels each, so that it could restart: a document's numbering.xml
    // grew by about 1.7 KB per list and ended up several times the size of its body. Lists of one
    // format now share a definition, and each restarts through the start override on its own
    // numbering instance, which is how Word itself writes a restarted list. Shared with table
    // cells by reference.
    internal Dictionary<NumberFormatValues, int> OrderedAbstractNumIds = [];
    internal int NextNumId;
    internal int? ListNumId;
    internal int? ListIlvl;
    internal bool ListInside;

    // Depth of <li> elements currently being processed. A block child (<li><p>x</p></li>) makes
    // BuildElement flush before its own children run, and that flush would otherwise clear the
    // list state BuildListItem just set, losing the bullet. While this is non-zero an empty flush
    // leaves the list state alone so it still reaches the paragraph the child produces.
    internal int ListItemDepth;

    // How many runs the current list item had once its marker was written. While CurrentRuns is
    // still this length the item has no content of its own yet, so a block child joins the marker's
    // paragraph instead of leaving it stranded. Saved and restored around nested items.
    internal int ListItemContentFloor;

    // The run holding the text marker ("● " or "1. ") of the list item being built, when the list
    // has no real numbering. Its trailing space is part of the marker and is not trimmed where the
    // paragraph ends, as a folded space is.
    internal Run? ListMarker;

    // A page break owed to a paragraph that has not been built yet. Word has no "break after", so
    // page-break-before and page-break-after both become a pageBreakBefore on a paragraph: its own
    // for the former, the following one for the latter. An empty flush leaves this pending, so a
    // block that produces no paragraph passes the break on rather than swallowing it.
    internal bool PendingPageBreak;

    // Whether the text emitted so far ends in a space, so whitespace spanning an inline boundary
    // folds the way a browser folds it. Cleared whenever a paragraph is flushed.
    //
    // It starts out set, and is set again by every flush: whitespace that opens a paragraph folds
    // away like whitespace after a space, since a browser draws none at the start of a line.
    internal bool LastWasSpace = true;
}
