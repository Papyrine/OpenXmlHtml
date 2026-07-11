namespace OpenXmlHtml;

/// <summary>
/// Mints an explicit, deterministic relationship id for a document-level part. <c>AddNewPart</c>'s
/// default id is random, which breaks deterministic packaging for consumers relying on reproducible
/// output.
/// </summary>
static class PartRelationshipId
{
    /// <summary>
    /// Prefers the bare <paramref name="baseName" /> (e.g. <c>rNumbering</c>, <c>rStyles</c>), falling back
    /// to <c>{baseName}1</c>, <c>{baseName}2</c>, ... only if it is already taken.
    /// </summary>
    internal static string Next(MainDocumentPart main, string baseName)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in main.Parts)
        {
            used.Add(pair.RelationshipId);
        }

        foreach (var relationship in main.ExternalRelationships)
        {
            used.Add(relationship.Id);
        }

        foreach (var relationship in main.HyperlinkRelationships)
        {
            used.Add(relationship.Id);
        }

        foreach (var relationship in main.DataPartReferenceRelationships)
        {
            used.Add(relationship.Id);
        }

        if (!used.Contains(baseName))
        {
            return baseName;
        }

        var index = 1;
        while (used.Contains($"{baseName}{index}"))
        {
            index++;
        }

        return $"{baseName}{index}";
    }
}
