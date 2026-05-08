enum FloatSide
{
    None,
    Left,
    Right
}

record ImageData(byte[] Bytes, string ContentType, int? WidthPx, int? HeightPx)
{
    internal FloatSide Float { get; init; }
}
