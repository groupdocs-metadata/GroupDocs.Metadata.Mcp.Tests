using GroupDocs.Metadata.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Metadata.Mcp.IntegrationTests;

/// WriteMetadata calls Save(), which is blocked in evaluation mode. Tests branch on
/// whether GROUPDOCS_LICENSE_PATH is set (same pattern as RemoveMetadataTests). The
/// unknown-property path returns before Save and is asserted unconditionally.
[Collection(McpServerCollection.Name)]
public class WriteMetadataTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public WriteMetadataTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private static bool IsLicensed =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROUPDOCS_LICENSE_PATH"));

    [Fact]
    public async Task WriteMetadata_UnknownProperty_ReturnsDescriptiveError()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Write.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["property"] = "NotARealProperty",
                ["value"] = "x",
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("Metadata write failed for", body, StringComparison.Ordinal);
        Assert.Contains("Unknown property", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteMetadata_InEvaluationMode_ReturnsDescriptiveFailure()
    {
        if (IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH is set — skipping evaluation-mode assertion.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Write.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["property"] = "Author",
                ["value"] = "MCP Integration Test",
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        // Author is applicable to PDF, so the write applies in-memory and Save() then
        // throws the eval-only exception, surfaced via the descriptive prefix.
        Assert.Contains("Metadata write failed for", body, StringComparison.Ordinal);
        Assert.Contains("Evaluation only", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WriteMetadata_Author_WritesAndReadsBack_Licensed()
    {
        if (!IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH not set — skipping licensed-mode test.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);
        const string newAuthor = "MCP Write Test";

        var writeResponse = await _fixture.Client.CallToolAsync(
            catalog.Write.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["property"] = "Author",
                ["value"] = newAuthor,
            });

        var writeBody = ToolResponse.Text(writeResponse);
        _output.WriteLine(writeBody);
        Assert.False(writeResponse.IsError ?? false, $"Write failed: {writeBody}");
        Assert.DoesNotContain("Metadata write failed for", writeBody, StringComparison.Ordinal);

        // Read the produced *_updated.pdf back and confirm the new author is present.
        var searchResponse = await _fixture.Client.CallToolAsync(
            catalog.Search.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = "authored_updated.pdf" },
                ["nameContains"] = "author",
            });

        var searchBody = ToolResponse.Text(searchResponse);
        _output.WriteLine(searchBody);
        Assert.Contains(newAuthor, searchBody, StringComparison.Ordinal);
    }
}
