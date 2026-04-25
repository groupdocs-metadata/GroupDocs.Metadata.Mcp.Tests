using System.Text.Json;
using GroupDocs.Metadata.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Metadata.Mcp.IntegrationTests;

[Collection(McpServerCollection.Name)]
public class ReadMetadataTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ReadMetadataTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ReadMetadata_AuthoredPdf_ReturnsFileFormatAndProperties()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Read.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        // PDF responses can exceed the tool's output budget and be truncated mid-JSON,
        // so we verify by substring rather than by full JsonDocument.Parse.
        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("\"fileFormat\"", body);
        Assert.Contains("Pdf", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/pdf", body);
        Assert.Contains("\"properties\"", body);
    }

    [Fact]
    public async Task ReadMetadata_Jpeg_ReturnsJpegFormat()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Read.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.PlainJpeg },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.Equal("Jpeg", json.GetProperty("fileFormat").GetString(), ignoreCase: true);
        Assert.Equal("image/jpeg", json.GetProperty("mimeType").GetString());
    }

    [Fact]
    public async Task ReadMetadata_AuthoredPdf_SurfacesKnownAuthorValue()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Read.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
            });

        var body = ToolResponse.Text(response);

        // The tool returns a dictionary of metadata-package → list of { name, value }.
        // Either the raw author or the raw title written in the fixture must surface somewhere.
        Assert.True(
            body.Contains(SampleDocuments.KnownAuthor, StringComparison.Ordinal) ||
            body.Contains(SampleDocuments.KnownTitle, StringComparison.Ordinal),
            $"Expected to find '{SampleDocuments.KnownAuthor}' or '{SampleDocuments.KnownTitle}' in response:\n{body}");
    }

    public static IEnumerable<object[]> RealSampleData() => new[]
    {
        new object[] { SampleDocuments.SamplePdf,  "application/pdf" },
        new object[] { SampleDocuments.SampleJpeg, "image/jpeg" },
        new object[] { SampleDocuments.SamplePng,  "image/png" },
        new object[] { SampleDocuments.SampleDocx, "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        new object[] { SampleDocuments.SampleXlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
    };

    [Theory]
    [MemberData(nameof(RealSampleData))]
    public async Task ReadMetadata_RealSample_ReportsExpectedMimeType(string fileName, string expectedMimeType)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.Read.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error reading '{fileName}': {ToolResponse.Text(response)}");

        // OOXML and PDF responses can exceed the tool's output budget and be truncated
        // mid-JSON, so we verify by substring rather than full JsonDocument.Parse.
        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("\"fileFormat\"", body);
        Assert.Contains(expectedMimeType, body, StringComparison.OrdinalIgnoreCase);
    }
}
