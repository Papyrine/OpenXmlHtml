[TestFixture]
public class WordNumberingTests
{
    [Test]
    public void EnsureListDefinitions_SeedsBulletAndDecimal()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordNumbering.EnsureListDefinitions(main);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        Assert.That(Formats(numbering), Is.EquivalentTo([NumberFormatValues.Bullet, NumberFormatValues.Decimal]));

        // Word can only apply a definition that an instance points at.
        foreach (var abstractNum in numbering.Elements<AbstractNum>())
        {
            var abstractNumId = abstractNum.AbstractNumberId!.Value;
            Assert.That(
                numbering.Elements<NumberingInstance>().Any(_ => _.GetFirstChild<AbstractNumId>()?.Val?.Value == abstractNumId),
                Is.True,
                $"abstractNum {abstractNumId} has no numbering instance");
        }
    }

    [Test]
    public void EnsureListDefinitions_IsIdempotent()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordNumbering.EnsureListDefinitions(main);
        WordNumbering.EnsureListDefinitions(main);
        WordNumbering.EnsureListDefinitions(main);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        Assert.That(numbering.Elements<AbstractNum>().Count(), Is.EqualTo(2));
        Assert.That(numbering.Elements<NumberingInstance>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void EnsureListDefinitions_UsesDeterministicRelationshipId()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordNumbering.EnsureListDefinitions(main);

        Assert.That(main.GetIdOfPart(main.NumberingDefinitionsPart!), Is.EqualTo("rNumbering"));
    }

    /// <summary>
    /// Seeding after a conversion must not add a second bullet definition — the converted list already
    /// supplies one — but the document has no ordered list, so a decimal definition is still added.
    /// </summary>
    [Test]
    public void EnsureListDefinitions_AfterBulletConversion_ReusesTheConvertedDefinition()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx("<ul><li>Alpha</li></ul>", stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;
        var before = main.NumberingDefinitionsPart!.Numbering!.Elements<AbstractNum>().Count();

        WordNumbering.EnsureListDefinitions(main);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        Assert.That(Formats(numbering).Count(_ => _ == NumberFormatValues.Bullet), Is.EqualTo(1));
        Assert.That(numbering.Elements<AbstractNum>().Count(), Is.EqualTo(before + 1));
    }

    /// <summary>
    /// A document whose conversion already produced both formats needs nothing seeded.
    /// </summary>
    [Test]
    public void EnsureListDefinitions_AfterBulletAndOrderedConversion_AddsNothing()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx("<ul><li>Alpha</li></ul><ol><li>First</li></ol>", stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, true);
        var main = document.MainDocumentPart!;
        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        var abstractNums = numbering.Elements<AbstractNum>().Count();
        var instances = numbering.Elements<NumberingInstance>().Count();

        WordNumbering.EnsureListDefinitions(main);

        Assert.That(numbering.Elements<AbstractNum>().Count(), Is.EqualTo(abstractNums));
        Assert.That(numbering.Elements<NumberingInstance>().Count(), Is.EqualTo(instances));
    }

    /// <summary>
    /// Seeded ids must not collide with each other: <c>GetNextId</c> spans both abstract definitions and
    /// numbering instances.
    /// </summary>
    [Test]
    public void EnsureListDefinitions_AllocatesUniqueIds()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordNumbering.EnsureListDefinitions(main);

        var numbering = main.NumberingDefinitionsPart!.Numbering!;
        var abstractNumIds = numbering.Elements<AbstractNum>().Select(_ => _.AbstractNumberId!.Value).ToList();
        var numIds = numbering.Elements<NumberingInstance>().Select(_ => _.NumberID!.Value).ToList();
        Assert.That(abstractNumIds, Is.Unique);
        Assert.That(numIds, Is.Unique);
        Assert.That(abstractNumIds.Intersect(numIds), Is.Empty);
    }

    static IEnumerable<NumberFormatValues> Formats(Numbering numbering) =>
        numbering
            .Elements<AbstractNum>()
            .Select(_ => _.Elements<Level>().First(level => level.LevelIndex?.Value == 0))
            .Select(_ => _.NumberingFormat!.Val!.Value);
}
