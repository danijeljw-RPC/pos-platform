namespace DaxaPos.UnitTests.Architecture;

/// <summary>
/// Guard test for ADR-0015 §Risks (PLAN-0003 Milestone G): the <c>IgnoreQueryFilters()</c> escape
/// hatch through the fail-closed tenant query filters is allowed only in the narrow, documented
/// bootstrap/authentication locations — the places that must run before a tenant context exists.
/// This test scans the production source so a new, undocumented bypass (or a removed documented
/// one) fails the build until this list, and the bootstrap-callers comment in
/// <c>DaxaDbContext.cs</c>, are consciously updated together.
/// </summary>
public class IgnoreQueryFiltersUsageTests
{
    /// <summary>
    /// The documented call-site files (see the query-filter comment in
    /// <c>src/DaxaPos.Persistence/DaxaDbContext.cs</c>):
    /// bootstrap admin seeding, email login (tenant unknown until the user resolves), session and
    /// device-token validation (tenant comes from the resolved session/credential row), and
    /// pre-auth device registration (tenant comes from the matched PIN row).
    /// </summary>
    private static readonly IReadOnlyList<string> ApprovedFiles =
    [
        "src/DaxaPos.Api/Authentication/DeviceTokenAuthenticationHandler.cs",
        "src/DaxaPos.Api/Authentication/SessionAuthenticationHandler.cs",
        "src/DaxaPos.Api/BootstrapAdminSeeder.cs",
        "src/DaxaPos.Api/Endpoints/Identity/AuthEndpoints.cs",
        "src/DaxaPos.Api/Endpoints/Identity/DeviceRegistrationEndpoints.cs",
    ];

    [Fact]
    public void IgnoreQueryFilters_AppearsOnly_InTheDocumentedBootstrapLocations()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");

        var filesUsingTheEscapeHatch = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildArtifact(path))
            // The leading dot matches only real invocations, not prose mentions in comments.
            .Where(path => File.ReadAllText(path).Contains(".IgnoreQueryFilters("))
            .Select(path => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        // Exact equality in both directions: an unapproved new bypass fails, and so does removing
        // a documented one — either way this list and the DaxaDbContext comment must be updated
        // deliberately, not drift.
        Assert.Equal(ApprovedFiles, filesUsingTheEscapeHatch);
    }

    private static bool IsBuildArtifact(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DaxaPos.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }
}
