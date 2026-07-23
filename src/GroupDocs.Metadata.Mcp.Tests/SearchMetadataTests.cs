using GroupDocs.Metadata.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Metadata.Mcp.IntegrationTests;

/// SearchMetadata is read-only (no Save), so all of these run in evaluation mode.
public class SearchMetadataTests : IClassFixture<McpServerFixture>
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SearchMetadataTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task SearchMetadata_ByName_FindsAuthorWithKnownValue()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Search.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["nameContains"] = "author",
            });

        Assert.False(response.IsError ?? false, $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.True(json.GetProperty("count").GetInt32() >= 1, "Expected at least one author property.");
        Assert.Contains(SampleDocuments.KnownAuthor, ToolResponse.Text(response), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchMetadata_ByCategoryPerson_ReturnsMatches()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Search.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["category"] = "person",
            });

        Assert.False(response.IsError ?? false, $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.True(json.GetProperty("count").GetInt32() >= 1);
        // every returned property should carry the requested category label
        foreach (var p in json.GetProperty("properties").EnumerateArray())
            Assert.Equal("Person", p.GetProperty("category").GetString());
    }

    [Fact]
    public async Task SearchMetadata_ValueMismatch_ReturnsZeroCount()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Search.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
                ["nameContains"] = "author",
                ["valueContains"] = "this-value-does-not-exist-xyz",
            });

        Assert.False(response.IsError ?? false, $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        Assert.Equal(0, json.GetProperty("count").GetInt32());
    }
}
