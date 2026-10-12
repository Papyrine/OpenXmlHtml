// An image part a conversion has already written: the relationship that reaches it, the image
// index it was written under, and how many drawings have reused it since.
class StoredImage(string relationshipId, int index)
{
    internal string RelationshipId { get; } = relationshipId;
    internal int Index { get; } = index;
    internal int Reuses { get; set; }
}
