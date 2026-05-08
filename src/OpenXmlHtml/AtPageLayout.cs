class AtPageLayout
{
    internal int? WidthTwips { get; set; }
    internal int? HeightTwips { get; set; }
    internal int? MarginTopTwips { get; set; }
    internal int? MarginRightTwips { get; set; }
    internal int? MarginBottomTwips { get; set; }
    internal int? MarginLeftTwips { get; set; }
    internal bool Landscape { get; set; }
    internal int? ColumnCount { get; set; }

    internal bool IsEmpty =>
        WidthTwips == null &&
        HeightTwips == null &&
        MarginTopTwips == null &&
        MarginRightTwips == null &&
        MarginBottomTwips == null &&
        MarginLeftTwips == null &&
        !Landscape &&
        ColumnCount == null;
}
