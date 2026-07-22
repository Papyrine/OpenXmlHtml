enum FloatSide
{
    None,
    Left,
    Right
}

record ImageData(byte[] Bytes, string ContentType, int? WidthPx, int? HeightPx)
{
    internal FloatSide Float { get; init; }

    // The element this came from, so a drop can be reported against the tag the author wrote. The
    // flat segment path erases the distinction otherwise: an <svg> and an <img> both arrive as an
    // image segment.
    internal string SourceTag { get; init; } = "img";
}
