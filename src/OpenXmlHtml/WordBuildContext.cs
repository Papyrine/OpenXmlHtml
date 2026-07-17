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

    // Whether the text emitted so far ends in a space, so whitespace spanning an inline boundary
    // folds the way a browser folds it. Cleared whenever a paragraph is flushed.
    internal bool LastWasSpace;
    internal int? ReversedStart;
}
