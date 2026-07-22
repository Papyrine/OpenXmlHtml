class WordBuildContext
{
    internal List<OpenXmlElement> CurrentRuns = [];
    internal int ImageIndex;
    internal int ListDepth;
    internal int HeadingLevel;
    internal int FootnoteIndex;
    internal int BookmarkId;
    internal MainDocumentPart? MainPart;
    internal HtmlConvertSettings? Settings;
    internal Dictionary<string, StyleType>? StyleMap;
    internal string? ParagraphStyleId;
    internal ParagraphFormatState? ParagraphFormat;
    internal bool ParagraphRightToLeft;
    internal Stack<(int NumId, int Ilvl, bool IsOrdered, bool Inside, bool NoMarker)> ListStack = new();
    internal int? BulletAbstractNumId;
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

    // A page break owed to a paragraph that has not been built yet. Word has no "break after", so
    // page-break-before and page-break-after both become a pageBreakBefore on a paragraph: its own
    // for the former, the following one for the latter. An empty flush leaves this pending, so a
    // block that produces no paragraph passes the break on rather than swallowing it.
    internal bool PendingPageBreak;

    // Whether the text emitted so far ends in a space, so whitespace spanning an inline boundary
    // folds the way a browser folds it. Cleared whenever a paragraph is flushed.
    internal bool LastWasSpace;
    internal int? ReversedStart;
}
