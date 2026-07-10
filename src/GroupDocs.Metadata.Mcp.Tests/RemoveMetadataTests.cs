using GroupDocs.Metadata.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Metadata.Mcp.IntegrationTests;

/// RemoveMetadata calls GroupDocs.Metadata's Save(), which is blocked in
/// evaluation mode by the underlying library (throws "Could not save the file.
/// Evaluation only."). As of MCP 26.7.0 the tool catches that exception and
/// returns a descriptive "Metadata removal failed for '<file>': ..." string
/// (IsError is false) rather than surfacing an MCP invocation error. Tests
/// therefore branch on whether GROUPDOCS_LICENSE_PATH is set on the test host
/// and propagated to the fixture.
[Collection(McpServerCollection.Name)]
public class RemoveMetadataTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public RemoveMetadataTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private static bool IsLicensed =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROUPDOCS_LICENSE_PATH"));

    [Fact]
    public async Task RemoveMetadata_InEvaluationMode_ReturnsDescriptiveFailure()
    {
        if (IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH is set — skipping evaluation-mode assertion.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Remove.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.PlainJpeg },
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine($"Eval-mode response (expected): {body}");

        // As of MCP 26.7.0 the tool catches Save()'s eval-mode exception and returns a
        // descriptive string (IsError is false). The response must start with the
        // failure prefix and mention the underlying evaluation-only cause.
        Assert.Contains("Metadata removal failed for", body, StringComparison.Ordinal);
        Assert.Contains("Evaluation only", body, StringComparison.OrdinalIgnoreCase);

        // Server stays alive for subsequent calls.
        var listAfter = await _fixture.Client.ListToolsAsync();
        Assert.NotEmpty(listAfter);
    }

    [Fact]
    public async Task RemoveMetadata_Jpeg_WritesCleanOutput_Licensed()
    {
        if (!IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH not set — skipping licensed-mode test.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Remove.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.PlainJpeg },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        var cleanPath = Path.Combine(_fixture.StoragePath, "photo_clean.jpg");
        Assert.True(File.Exists(cleanPath),
            $"Expected cleaned file at '{cleanPath}'. Response body:\n{body}");

        Assert.Contains("photo_clean", body, StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> RealRemovableSamples() => new[]
    {
        new object[] { SampleDocuments.SamplePdf,  "sample_clean.pdf" },
        new object[] { SampleDocuments.SampleJpeg, "sample_clean.jpg" },
        new object[] { SampleDocuments.SamplePng,  "sample_clean.png" },
        new object[] { SampleDocuments.SampleDocx, "sample_clean.docx" },
        new object[] { SampleDocuments.SampleXlsx, "sample_clean.xlsx" },
    };

    [Theory]
    [MemberData(nameof(RealRemovableSamples))]
    public async Task RemoveMetadata_RealSample_WritesCleanOutput_Licensed(string fileName, string expectedCleanFileName)
    {
        if (!IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH not set — skipping licensed-mode test.");
            return;
        }

        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Remove.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
            });

        Assert.False(response.IsError ?? false,
            $"Remove failed for '{fileName}': {ToolResponse.Text(response)}");

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        var cleanPath = Path.Combine(_fixture.StoragePath, expectedCleanFileName);
        Assert.True(File.Exists(cleanPath),
            $"Expected cleaned file at '{cleanPath}'. Response body:\n{body}");
    }

    [Fact]
    public async Task RemoveMetadata_FollowUpReadOfCleanedFile_Succeeds_Licensed()
    {
        if (!IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH not set — skipping licensed-mode test.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var removeResponse = await _fixture.Client.CallToolAsync(
            catalog.Remove.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.PlainJpeg },
            });

        Assert.False(removeResponse.IsError ?? false,
            $"Remove failed: {ToolResponse.Text(removeResponse)}");

        var readResponse = await _fixture.Client.CallToolAsync(
            catalog.Read.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = "photo_clean.jpg" },
            });

        Assert.False(readResponse.IsError ?? false,
            $"Read-back of cleaned file failed: {ToolResponse.Text(readResponse)}");

        var body = ToolResponse.Text(readResponse);
        _output.WriteLine(body);

        Assert.Contains("image/jpeg", body);
    }

    [Fact]
    public async Task RemoveMetadata_SelectiveCategory_InEvaluationMode_ReturnsDescriptiveFailure()
    {
        if (IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH is set — skipping evaluation-mode assertion.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        // authored.pdf has author-tagged properties, so the selective removal applies
        // in-memory and Save() then throws the eval-only exception.
        var response = await _fixture.Client.CallToolAsync(
            catalog.Remove.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["categories"] = new[] { "author" },
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("Metadata removal failed for", body, StringComparison.Ordinal);
        Assert.Contains("Evaluation only", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveMetadata_SelectiveAuthor_RemovesOnlyAuthor_Licensed()
    {
        if (!IsLicensed)
        {
            _output.WriteLine("GROUPDOCS_LICENSE_PATH not set — skipping licensed-mode test.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var removeResponse = await _fixture.Client.CallToolAsync(
            catalog.Remove.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["categories"] = new[] { "author" },
            });

        var removeBody = ToolResponse.Text(removeResponse);
        _output.WriteLine(removeBody);
        Assert.False(removeResponse.IsError ?? false, $"Selective remove failed: {removeBody}");

        // The cleaned file should no longer surface the known author under a person search.
        var searchResponse = await _fixture.Client.CallToolAsync(
            catalog.Search.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = "authored_clean.pdf" },
                ["nameContains"] = "author",
                ["valueContains"] = SampleDocuments.KnownAuthor,
            });

        var searchBody = ToolResponse.Text(searchResponse);
        _output.WriteLine(searchBody);
        Assert.DoesNotContain(SampleDocuments.KnownAuthor, searchBody, StringComparison.Ordinal);
    }
}
