struct FormatState
{
    // Bold, Italic, SmallCaps and Shadow are tri-state: null means "not specified", so the run
    // inherits from its paragraph style or from an enclosing element. false means "explicitly off"
    // (font-weight:normal, font-style:normal, font-variant:normal, text-shadow:none) and has to
    // reach Word as <w:b w:val="0"/> and its equivalents to override what would otherwise be
    // inherited — the absence of the element inherits instead.
    //
    // Strikethrough stays a plain bool deliberately: css text-decoration propagates to descendants
    // and cannot be cancelled by a descendant's text-decoration:none, so absent-means-off is the
    // correct model there.
    internal bool? Bold { get; set; }
    internal bool? Italic { get; set; }
    internal UnderlineValues? UnderlineStyle { get; set; }
    internal bool Strikethrough { get; set; }
    internal string? Color { get; set; }
    internal double? FontSizePt { get; set; }
    internal string? FontFamily { get; set; }
    internal bool Superscript { get; set; }
    internal bool Subscript { get; set; }
    internal ImageData? Image { get; set; }
    internal int ListDepth { get; set; }
    internal string? LinkUrl { get; set; }
    internal string? LinkTitle { get; set; }
    internal string? RunStyleId { get; set; }
    internal string? BackgroundColor { get; set; }
    internal BorderInfo? Border { get; set; }
    internal bool? SmallCaps { get; set; }
    internal string? TextTransform { get; set; }
    internal bool? Shadow { get; set; }
    internal int? CharacterSpacingTwips { get; set; }
    internal bool NoWrap { get; set; }
    internal string? UnderlineColor { get; set; }
    internal int? CharacterScale { get; set; }
    internal bool RightToLeft { get; set; }

    internal readonly bool HasFormatting =>
        Bold != null ||
        Italic != null ||
        UnderlineStyle != null ||
        Strikethrough ||
        Superscript ||
        Subscript ||
        Color != null ||
        FontSizePt != null ||
        FontFamily != null ||
        RunStyleId != null ||
        BackgroundColor != null ||
        Border != null ||
        SmallCaps != null ||
        Shadow != null ||
        CharacterSpacingTwips != null ||
        UnderlineColor != null ||
        CharacterScale != null ||
        RightToLeft;
}
