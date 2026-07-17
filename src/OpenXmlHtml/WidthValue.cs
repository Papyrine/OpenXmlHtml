enum WidthUnit
{
    Twips,
    Percent
}

// A css width that kept its unit. Percentages cannot be flattened to twips without knowing the
// containing width, and Word can express them directly (w:type="pct"), so the unit has to survive
// parsing for the caller to emit the right thing.
record struct WidthValue(int Value, WidthUnit Unit);
