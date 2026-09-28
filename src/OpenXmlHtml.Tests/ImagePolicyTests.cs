public class ImagePolicyTests
{
    [Test]
    public async Task Deny_RejectsAll()
    {
        var policy = ImagePolicy.Deny();
        await Assert.That(policy.IsAllowed("https://example.com/img.png")).IsFalse();
        await Assert.That(policy.IsAllowed(@"C:\Images\photo.png")).IsFalse();
    }

    [Test]
    public async Task AllowAll_AcceptsAll()
    {
        var policy = ImagePolicy.AllowAll();
        await Assert.That(policy.IsAllowed("https://example.com/img.png")).IsTrue();
        await Assert.That(policy.IsAllowed(@"C:\Images\photo.png")).IsTrue();
    }

    [Test]
    public async Task SafeDomains_ExactMatch()
    {
        var policy = ImagePolicy.SafeDomains("example.com");
        await Assert.That(policy.IsAllowed("https://example.com/img.png")).IsTrue();
        await Assert.That(policy.IsAllowed("https://other.com/img.png")).IsFalse();
    }

    [Test]
    public async Task SafeDomains_SubdomainMatch()
    {
        var policy = ImagePolicy.SafeDomains("example.com");
        await Assert.That(policy.IsAllowed("https://images.example.com/img.png")).IsTrue();
        await Assert.That(policy.IsAllowed("https://cdn.images.example.com/img.png")).IsTrue();
    }

    [Test]
    public async Task SafeDomains_RejectsPartialMatch()
    {
        var policy = ImagePolicy.SafeDomains("example.com");
        await Assert.That(policy.IsAllowed("https://notexample.com/img.png")).IsFalse();
        await Assert.That(policy.IsAllowed("https://example.com.evil.com/img.png")).IsFalse();
    }

    [Test]
    public async Task SafeDirectories_AllowsMatchingPath()
    {
        var policy = ImagePolicy.SafeDirectories(Path.GetTempPath());
        var testPath = Path.Combine(Path.GetTempPath(), "test.png");
        await Assert.That(policy.IsAllowed(testPath)).IsTrue();
    }

    [Test]
    public async Task SafeDirectories_RejectsNonMatchingPath()
    {
        var safeDir = Path.Combine(Path.GetTempPath(), "safe_dir_test");
        var policy = ImagePolicy.SafeDirectories(safeDir);
        var testPath = Path.Combine(Path.GetTempPath(), "unsafe", "test.png");
        await Assert.That(policy.IsAllowed(testPath)).IsFalse();
    }

    [Test]
    public async Task SafeDirectories_PathTraversalProtection()
    {
        var safeDir = Path.Combine(Path.GetTempPath(), "safe_dir_test");
        var policy = ImagePolicy.SafeDirectories(safeDir);
        var traversalPath = Path.Combine(safeDir, "..", "unsafe", "test.png");
        await Assert.That(policy.IsAllowed(traversalPath)).IsFalse();
    }

    [Test]
    public async Task SafeDirectories_RejectsMalformedFileUri()
    {
        var policy = ImagePolicy.SafeDirectories(Path.GetTempPath());
        await Assert.That(policy.IsAllowed("file:///%")).IsFalse();
    }

    [Test]
    public async Task SafeDirectories_RejectsInvalidPath()
    {
        var policy = ImagePolicy.SafeDirectories(Path.GetTempPath());
        await Assert.That(policy.IsAllowed("\0invalid")).IsFalse();
    }

    [Test]
    public async Task Filter_CustomPredicate()
    {
        var policy = ImagePolicy.Filter(src => src.Contains("allowed"));
        await Assert.That(policy.IsAllowed("https://allowed.example.com/img.png")).IsTrue();
        await Assert.That(policy.IsAllowed("https://denied.example.com/img.png")).IsFalse();
    }
}
