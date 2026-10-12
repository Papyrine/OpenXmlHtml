// Works out the size an image is drawn at, in pixels, from what the html gave and what the image
// itself is.
//
// An <img> may give both dimensions, one, or neither. Each missing one used to default to 100, so
// an image with only a width was squashed or stretched to 100 high, and one with no size at all
// was drawn as a 100 by 100 square whatever its shape. A browser takes the missing dimension from
// the image: its natural size, or the given dimension scaled by its natural proportions. The
// header of the common raster formats says what that size is, so it is read from there.
static class ImageSize
{
    // What a missing dimension falls back to when the image's own size cannot be read.
    const int fallback = 100;

    // The widest an image is drawn at its natural size: the text width of an A4 page with one-inch
    // margins. A browser lets a large image run off the side of the page. On paper that is lost
    // content, so an image nobody gave a size to is scaled down to fit instead.
    const int maxNaturalWidth = 602;

    internal static (int Width, int Height) Resolve(ImageData image)
    {
        if (image is { WidthPx: { } givenWidth, HeightPx: { } givenHeight })
        {
            return (givenWidth, givenHeight);
        }

        if (!TryReadNaturalSize(image.Bytes, out var naturalWidth, out var naturalHeight))
        {
            return (image.WidthPx ?? fallback, image.HeightPx ?? fallback);
        }

        if (image.WidthPx is { } width)
        {
            return (width, Scale(width, naturalHeight, naturalWidth));
        }

        if (image.HeightPx is { } height)
        {
            return (Scale(height, naturalWidth, naturalHeight), height);
        }

        if (naturalWidth > maxNaturalWidth)
        {
            return (maxNaturalWidth, Scale(maxNaturalWidth, naturalHeight, naturalWidth));
        }

        return (naturalWidth, naturalHeight);
    }

    static int Scale(int given, int numerator, int denominator) =>
        Math.Max(1, (int) Math.Round((double) given * numerator / denominator));

    // The pixel size recorded in the image's header. False for a format not read here, a file too
    // short to hold one, or a size of zero, all of which leave the caller on its fallback.
    internal static bool TryReadNaturalSize(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = 0;
        height = 0;
        var read = TryReadPng(bytes, ref width, ref height) ||
                   TryReadGif(bytes, ref width, ref height) ||
                   TryReadBmp(bytes, ref width, ref height) ||
                   TryReadJpeg(bytes, ref width, ref height);
        return read &&
               width > 0 &&
               height > 0;
    }

    // Signature, then the IHDR chunk: width and height as big-endian 32-bit at 16 and 20.
    static bool TryReadPng(ReadOnlySpan<byte> bytes, ref int width, ref int height)
    {
        if (bytes.Length < 24 ||
            bytes[0] != 0x89 ||
            bytes[1] != 'P' ||
            bytes[2] != 'N' ||
            bytes[3] != 'G')
        {
            return false;
        }

        width = BigEndian32(bytes, 16);
        height = BigEndian32(bytes, 20);
        return true;
    }

    // "GIF87a" or "GIF89a", then the logical screen size as little-endian 16-bit at 6 and 8.
    static bool TryReadGif(ReadOnlySpan<byte> bytes, ref int width, ref int height)
    {
        if (bytes.Length < 10 ||
            bytes[0] != 'G' ||
            bytes[1] != 'I' ||
            bytes[2] != 'F')
        {
            return false;
        }

        width = bytes[6] | bytes[7] << 8;
        height = bytes[8] | bytes[9] << 8;
        return true;
    }

    // "BM", then the info header: width and height as little-endian 32-bit at 18 and 22. The height
    // is negative for a top-down bitmap.
    static bool TryReadBmp(ReadOnlySpan<byte> bytes, ref int width, ref int height)
    {
        if (bytes.Length < 26 ||
            bytes[0] != 'B' ||
            bytes[1] != 'M')
        {
            return false;
        }

        width = LittleEndian32(bytes, 18);
        height = Math.Abs(LittleEndian32(bytes, 22));
        return true;
    }

    // A jpeg is a run of segments, each a marker and a length. The size is in the start-of-frame
    // segment, which can come after any number of others, so they are walked until it turns up.
    static bool TryReadJpeg(ReadOnlySpan<byte> bytes, ref int width, ref int height)
    {
        if (bytes.Length < 4 ||
            bytes[0] != 0xFF ||
            bytes[1] != 0xD8)
        {
            return false;
        }

        var position = 2;
        while (position + 9 < bytes.Length)
        {
            if (bytes[position] != 0xFF)
            {
                return false;
            }

            var marker = bytes[position + 1];

            // Padding between segments.
            if (marker == 0xFF)
            {
                position++;
                continue;
            }

            // Start of frame, in any of its encodings. C4, C8 and CC share the range and are not.
            if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
            {
                height = bytes[position + 5] << 8 | bytes[position + 6];
                width = bytes[position + 7] << 8 | bytes[position + 8];
                return true;
            }

            // Markers that stand alone, with no length after them.
            if (marker is 0x01 or >= 0xD0 and <= 0xD8)
            {
                position += 2;
                continue;
            }

            var length = bytes[position + 2] << 8 | bytes[position + 3];
            if (length < 2)
            {
                return false;
            }

            position += 2 + length;
        }

        return false;
    }

    static int BigEndian32(ReadOnlySpan<byte> bytes, int offset) =>
        bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];

    static int LittleEndian32(ReadOnlySpan<byte> bytes, int offset) =>
        bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24;
}
