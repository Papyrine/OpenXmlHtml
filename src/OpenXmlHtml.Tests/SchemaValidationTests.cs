using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Validation;

/// <summary>
/// Holds every checked-in snapshot to the schema Word and Excel enforce.
/// </summary>
/// <remarks>
/// <see cref="WordValidationTests"/> validates one hand-written document, which reaches the cases
/// its author had in mind. The snapshots are what the suite actually produces across every
/// conversion it covers, so sweeping them reaches the markup nobody thought to construct.
/// <para>
/// Element order is what this catches. The OpenXML SDK appends a child at the end of its parent
/// wherever the schema declares it belongs, and Word repairs the result quietly, so a child left
/// out of sequence survives every snapshot comparison and surfaces only in a stricter consumer.
/// </para>
/// </remarks>
[TestFixture]
public class SchemaValidationTests
{
    [TestCaseSource(nameof(Snapshots))]
    public void SnapshotMatchesTheSchema(string snapshot)
    {
        var errors = Validate(Path.Combine(ProjectDirectory, snapshot));

        Assert.That(errors, Is.Empty, () => string.Join("\n", errors));
    }

    // A sweep that stops matching leaves nothing to run and nothing to report, which reads exactly
    // like a clean one.
    [Test]
    public void SnapshotsAreFound() =>
        Assert.That(Snapshots(), Is.Not.Empty);

    static List<string> Validate(string path)
    {
        var validator = new OpenXmlValidator();
        if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var book = SpreadsheetDocument.Open(path, false);
            return Describe(validator.Validate(book));
        }

        using var document = WordprocessingDocument.Open(path, false);
        return Describe(validator.Validate(document));
    }

    static List<string> Describe(IEnumerable<ValidationErrorInfo> errors) =>
        errors.Select(_ => $"{_.Description} ({_.Path?.XPath})")
            .ToList();

    public static IEnumerable<string> Snapshots() =>
        Directory.EnumerateFiles(ProjectDirectory, "*.verified.*", SearchOption.AllDirectories)
            .Where(_ => _.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                        _.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .Select(_ => Path.GetRelativePath(ProjectDirectory, _).Replace(Path.DirectorySeparatorChar, '/'))
            // Build output carries copies of the snapshots; they are the same files twice over.
            .Where(_ => !_.StartsWith("bin/", StringComparison.Ordinal) &&
                        !_.StartsWith("obj/", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    static string ProjectDirectory { get; } = Path.GetDirectoryName(SourcePath())!;

    static string SourcePath([CallerFilePath] string path = "") => path;
}
