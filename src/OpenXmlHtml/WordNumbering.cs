namespace OpenXmlHtml;

/// <summary>
/// Seeds list definitions into a document so Word can apply bullets and numbering to it.
/// </summary>
public static class WordNumbering
{
    /// <summary>
    /// Ensures <paramref name="main" /> has a numbering part carrying a bullet definition and a decimal
    /// definition, each with a numbering instance for Word to reference.
    /// </summary>
    /// <remarks>
    /// Word cannot apply a list unless a definition already exists: creating one writes to
    /// <c>word/numbering.xml</c>, a document-level part. Under <c>w:documentProtection w:edit="readOnly"</c>
    /// that part sits outside every editable range, so Word disables the bullet and numbering commands even
    /// inside a rich-text content control the user is allowed to edit. Seeding the definitions leaves Word
    /// merely referencing them, and the commands become available.
    ///
    /// Idempotent, and safe either side of a conversion: a definition of a given format is added only when
    /// the document has none, and new ids continue past the highest already in use. The numbering part is
    /// created with a deterministic relationship id, so output stays byte-reproducible.
    /// </remarks>
    public static void EnsureListDefinitions(MainDocumentPart main)
    {
        var part = WordNumberingBuilder.EnsureNumberingPart(main);
        var numbering = part.Numbering!;

        EnsureFormat(numbering, NumberFormatValues.Bullet);
        EnsureFormat(numbering, NumberFormatValues.Decimal);
    }

    static void EnsureFormat(Numbering numbering, NumberFormatValues format)
    {
        if (HasUsableDefinition(numbering, format))
        {
            return;
        }

        var abstractNumId = WordNumberingBuilder.GetNextId(numbering);
        if (format == NumberFormatValues.Bullet)
        {
            WordNumberingBuilder.CreateBulletAbstractNum(numbering, abstractNumId);
        }
        else
        {
            WordNumberingBuilder.CreateOrderedAbstractNum(numbering, abstractNumId, format);
        }

        WordNumberingBuilder.AddNumberingInstance(numbering, WordNumberingBuilder.GetNextId(numbering), abstractNumId);
    }

    /// <summary>
    /// An abstract definition of <paramref name="format" /> that some numbering instance points at. An
    /// abstract definition with no instance is not something Word can apply.
    /// </summary>
    static bool HasUsableDefinition(Numbering numbering, NumberFormatValues format)
    {
        foreach (var abstractNum in numbering.Elements<AbstractNum>())
        {
            if (abstractNum.AbstractNumberId?.Value is not { } abstractNumId)
            {
                continue;
            }

            var firstLevel = abstractNum
                .Elements<Level>()
                .FirstOrDefault(_ => _.LevelIndex?.Value == 0);
            if (firstLevel?.NumberingFormat?.Val?.Value != format)
            {
                continue;
            }

            foreach (var instance in numbering.Elements<NumberingInstance>())
            {
                if (instance.GetFirstChild<AbstractNumId>()?.Val?.Value == abstractNumId)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
