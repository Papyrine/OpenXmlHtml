// IsLineBreak distinguishes a <br> from the paragraph break <hr> emits, since both carry "\n" as
// their text. Word needs them to differ (<w:br/> vs a new <w:p>); Excel treats both as "\n" in an
// inline string, so the spreadsheet path can keep reading Text and ignore the flag.
//
// IsLinkUrl marks the " (url)" an anchor trails. That suffix is how a link's target survives in a
// cell, which holds one hyperlink at most and leaves rich text as the only way to say where the
// rest point. A Word paragraph can hold a hyperlink per anchor, so that path links the text and
// drops the suffix — but only when it managed to build the link, or the target would be lost.
record TextSegment(string Text, FormatState Format, bool IsLineBreak = false, bool IsLinkUrl = false);
