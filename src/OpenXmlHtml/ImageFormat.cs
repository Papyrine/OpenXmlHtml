// Works out which format an image is, so its part is declared as what it holds.
//
// The type an image arrives with is a claim: the media type of a data uri, the Content-Type a
// server sent, the extension of a file name. A server answers application/octet-stream, a file is
// renamed, a data uri is written as image/jpg. Any type that was not one of four known spellings
// used to be declared as png, so a gif, an emf or a webp was stored in a part named .png and
// declared image/png while holding something else. The leading bytes say what the image is, so
// they are read first and the claim is only the fallback.
static class ImageFormat
{
    internal const string Png = "image/png";
    internal const string Jpeg = "image/jpeg";
    internal const string Gif = "image/gif";
    internal const string Bmp = "image/bmp";
    internal const string Tiff = "image/tiff";
    internal const string Webp = "image/webp";
    internal const string Emf = "image/x-emf";
    internal const string Wmf = "image/x-wmf";
    internal const string Icon = "image/x-icon";
    internal const string Svg = "image/svg+xml";

    // The media types an image part is declared with, then the other spellings in use for them.
    // Pcx and jpeg 2000 are not detected from their bytes, and are stored as declared.
    static Dictionary<string, string> mediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [Png] = Png,
        [Jpeg] = Jpeg,
        [Gif] = Gif,
        [Bmp] = Bmp,
        [Tiff] = Tiff,
        [Webp] = Webp,
        [Emf] = Emf,
        [Wmf] = Wmf,
        [Icon] = Icon,
        [Svg] = Svg,
        ["image/x-pcx"] = "image/x-pcx",
        ["image/jp2"] = "image/jp2",

        ["image/x-png"] = Png,
        ["image/jpg"] = Jpeg,
        ["image/pjpeg"] = Jpeg,
        ["image/x-ms-bmp"] = Bmp,
        ["image/x-bmp"] = Bmp,
        ["image/tif"] = Tiff,
        ["image/emf"] = Emf,
        ["image/wmf"] = Wmf,
        ["image/vnd.microsoft.icon"] = Icon,
        ["image/ico"] = Icon,
        ["image/svg"] = Svg,
        ["image/pcx"] = "image/x-pcx"
    };

    static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];
    static ReadOnlySpan<byte> TiffLittleEndian => "II*\0"u8;
    static ReadOnlySpan<byte> TiffBigEndian => "MM\0*"u8;
    static ReadOnlySpan<byte> PlaceableWmf => [0xD7, 0xCD, 0xC6, 0x9A];
    static ReadOnlySpan<byte> IconSignature => [0x00, 0x00, 0x01, 0x00];
    static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    // The content type to declare the image part with: what the bytes are, else what they were
    // declared as, else png. Bytes that are no recognisable image under a type that names none
    // have nothing to go on, and stay on the png they have always been stored as.
    internal static string Resolve(ReadOnlySpan<byte> bytes, string? declared) =>
        Detect(bytes) ??
        FromMediaType(declared) ??
        Png;

    // The content type a declared media type stands for, or null where it names no image format
    // stored here. Only the media type is compared: "IMAGE/JPEG; charset=binary" is a jpeg.
    internal static string? FromMediaType(string? declared)
    {
        if (declared == null)
        {
            return null;
        }

        var parameters = declared.IndexOf(';');
        if (parameters >= 0)
        {
            declared = declared[..parameters];
        }

        return mediaTypes.GetValueOrDefault(declared.Trim());
    }

    // The format the leading bytes belong to, or null where they are none of the ones known here.
    internal static string? Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(PngSignature))
        {
            return Png;
        }

        if (bytes.StartsWith(JpegSignature))
        {
            return Jpeg;
        }

        if (bytes.StartsWith("GIF87a"u8) ||
            bytes.StartsWith("GIF89a"u8))
        {
            return Gif;
        }

        if (bytes.StartsWith(TiffLittleEndian) ||
            bytes.StartsWith(TiffBigEndian))
        {
            return Tiff;
        }

        // A riff container, whose form type sits after the four byte length.
        if (bytes.Length >= 12 &&
            bytes.StartsWith("RIFF"u8) &&
            bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return Webp;
        }

        if (IsBmp(bytes))
        {
            return Bmp;
        }

        // The first record is the header, type 1, which carries " EMF" at 40.
        if (bytes.Length >= 44 &&
            LittleEndian32(bytes, 0) == 1 &&
            bytes.Slice(40, 4).SequenceEqual(" EMF"u8))
        {
            return Emf;
        }

        if (IsWmf(bytes))
        {
            return Wmf;
        }

        if (IsIcon(bytes))
        {
            return Icon;
        }

        if (IsSvg(bytes))
        {
            return Svg;
        }

        return null;
    }

    // "BM" alone is two letters any text could start with, so the reserved bytes have to be zero
    // and the size of the info header one a bitmap has.
    static bool IsBmp(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 18 ||
            bytes[0] != 'B' ||
            bytes[1] != 'M' ||
            LittleEndian32(bytes, 6) != 0)
        {
            return false;
        }

        return LittleEndian32(bytes, 14) is 12 or 40 or 52 or 56 or 64 or 108 or 124;
    }

    // Either the placeable header most wmf files start with, or the bare metafile header: a type
    // of memory or disk, a header of nine words, and a version of Windows 1 or 3.
    static bool IsWmf(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(PlaceableWmf))
        {
            return true;
        }

        if (bytes.Length < 18)
        {
            return false;
        }

        return LittleEndian16(bytes, 0) is 1 or 2 &&
               LittleEndian16(bytes, 2) == 9 &&
               LittleEndian16(bytes, 4) is 0x0100 or 0x0300;
    }

    // The signature is mostly zeros, so the first directory entry is checked too: at least one
    // image, a reserved byte of zero, and a colour plane count of zero or one.
    static bool IsIcon(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 22 ||
            !bytes.StartsWith(IconSignature))
        {
            return false;
        }

        return LittleEndian16(bytes, 4) != 0 &&
               bytes[9] == 0 &&
               LittleEndian16(bytes, 10) is 0 or 1;
    }

    // Svg is text with no signature: what marks it is its root element. A byte order mark, an xml
    // declaration, comments and a doctype may all come before that.
    static bool IsSvg(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Utf8Bom))
        {
            bytes = bytes[3..];
        }

        while (true)
        {
            bytes = SkipWhitespace(bytes);
            int end;
            if (bytes.StartsWith("<?"u8))
            {
                end = bytes.IndexOf("?>"u8);
                if (end < 0)
                {
                    return false;
                }

                bytes = bytes[(end + 2)..];
                continue;
            }

            if (bytes.StartsWith("<!--"u8))
            {
                end = bytes.IndexOf("-->"u8);
                if (end < 0)
                {
                    return false;
                }

                bytes = bytes[(end + 3)..];
                continue;
            }

            if (bytes.StartsWith("<!"u8))
            {
                // A doctype with an internal subset closes with "]>".
                end = bytes.IndexOf((byte) '>');
                var subset = bytes.IndexOf((byte) '[');
                if (subset >= 0 &&
                    subset < end)
                {
                    end = bytes.IndexOf("]>"u8);
                    if (end >= 0)
                    {
                        end++;
                    }
                }

                if (end < 0)
                {
                    return false;
                }

                bytes = bytes[(end + 1)..];
                continue;
            }

            break;
        }

        if (bytes.StartsWith("<svg:svg"u8))
        {
            bytes = bytes[8..];
        }
        else if (bytes.StartsWith("<svg"u8))
        {
            bytes = bytes[4..];
        }
        else
        {
            return false;
        }

        // "<svg" has to be the whole name, not the start of a longer one.
        return bytes.Length > 0 &&
               (bytes[0] is (byte) '>' or (byte) '/' || IsWhitespace(bytes[0]));
    }

    static ReadOnlySpan<byte> SkipWhitespace(ReadOnlySpan<byte> bytes)
    {
        var index = 0;
        while (index < bytes.Length &&
               IsWhitespace(bytes[index]))
        {
            index++;
        }

        return bytes[index..];
    }

    static bool IsWhitespace(byte value) =>
        value is (byte) ' ' or (byte) '\t' or (byte) '\r' or (byte) '\n';

    static int LittleEndian16(ReadOnlySpan<byte> bytes, int offset) =>
        bytes[offset] | bytes[offset + 1] << 8;

    static int LittleEndian32(ReadOnlySpan<byte> bytes, int offset) =>
        bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24;
}
