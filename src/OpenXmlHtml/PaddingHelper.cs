static class PaddingHelper
{
    internal static (int? Top, int? Right, int? Bottom, int? Left)? TryParse(Dictionary<string, string> declarations)
    {
        int? top = null, right = null, bottom = null, left = null;
        var hasPadding = false;

        if (declarations.TryGetValue("padding", out var shorthand))
        {
            (top, right, bottom, left) = StyleParser.ParseMarginShorthand(shorthand);
            hasPadding = true;
        }

        if (declarations.TryGetValue("padding-top", out var pt))
        {
            top = StyleParser.ParseLengthToTwips(pt);
            hasPadding = true;
        }

        if (declarations.TryGetValue("padding-right", out var pr))
        {
            right = StyleParser.ParseLengthToTwips(pr);
            hasPadding = true;
        }

        if (declarations.TryGetValue("padding-bottom", out var pb))
        {
            bottom = StyleParser.ParseLengthToTwips(pb);
            hasPadding = true;
        }

        if (declarations.TryGetValue("padding-left", out var pl))
        {
            left = StyleParser.ParseLengthToTwips(pl);
            hasPadding = true;
        }

        if (!hasPadding)
        {
            return null;
        }

        return (top, right, bottom, left);
    }

    internal static T BuildMargin<T>(int? top, int? right, int? bottom, int? left)
        where T : OpenXmlCompositeElement, new()
    {
        var margin = new T();
        // CT_TcMar (w:tcMar) and CT_TblCellMar (w:tblCellMar) each accept two forms: the legacy
        // w:left/w:right and the Office 2010+ w:start/w:end. Both are schema-valid, Word reads
        // either and reports the same padding for them, and Morph reads both as well. This writes
        // the legacy pair because that is what Word itself writes when it saves.
        // LeftMargin and RightMargin are the CT_TcMar classes; in a w:tblCellMar the SDK's own
        // types are TableCellLeftMargin and TableCellRightMargin, but both serialise to the same
        // w:left/w:right, which is what lets one generic method cover both containers. The
        // difference shows only if the element is later read back through the typed property.
        // The order below — top, left, bottom, right — is the schema sequence for both types.
        if (top != null)
        {
            margin.Append(
                new TopMargin
                {
                    Width = top.Value.ToString(),
                    Type = TableWidthUnitValues.Dxa
                });
        }

        if (left != null)
        {
            margin.Append(
                new LeftMargin
                {
                    Width = left.Value.ToString(),
                    Type = TableWidthUnitValues.Dxa
                });
        }

        if (bottom != null)
        {
            margin.Append(
                new BottomMargin
                {
                    Width = bottom.Value.ToString(),
                    Type = TableWidthUnitValues.Dxa
                });
        }

        if (right != null)
        {
            margin.Append(
                new RightMargin
                {
                    Width = right.Value.ToString(),
                    Type = TableWidthUnitValues.Dxa
                });
        }

        return margin;
    }
}
