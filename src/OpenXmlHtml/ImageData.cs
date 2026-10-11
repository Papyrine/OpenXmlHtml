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

    // The image's alt text, for the outputs that cannot hold an image at all: a spreadsheet cell,
    // or a Word conversion with no MainDocumentPart to put the image part in. An image that failed
    // to resolve has always fallen back to its alt text; one that resolved and then had nowhere to
    // go left nothing, so whether the reader saw the description turned on whether the bytes
    // happened to load.
    internal string? AltText { get; init; }
}
