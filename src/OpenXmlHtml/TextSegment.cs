// IsLineBreak distinguishes a <br> from the paragraph break <hr> emits, since both carry "\n" as
// their text. Word needs them to differ (<w:br/> vs a new <w:p>); Excel treats both as "\n" in an
// inline string, so the spreadsheet path can keep reading Text and ignore the flag.
record TextSegment(string Text, FormatState Format, bool IsLineBreak = false);
