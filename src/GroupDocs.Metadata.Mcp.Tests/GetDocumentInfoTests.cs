using GroupDocs.Metadata.Mcp.IntegrationTests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace GroupDocs.Metadata.Mcp.IntegrationTests;

[Collection(McpServerCollection.Name)]
public class GetDocumentInfoTests
{
    private readonly McpServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GetDocumentInfoTests(McpServerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task GetDocumentInfo_AuthoredPdf_ReportsPdfFormatAndPageCount()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error: {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.Equal("Pdf", json.GetProperty("fileFormat").GetString(), ignoreCase: true);
        Assert.Equal("application/pdf", json.GetProperty("mimeType").GetString());
        Assert.True(json.GetProperty("pageCount").GetInt32() >= 1);
        Assert.False(json.GetProperty("isEncrypted").GetBoolean());
    }

    [Fact]
    public async Task GetDocumentInfo_Jpeg_ReportsJpegFormat()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
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
    public async Task GetDocumentInfo_DoesNotEnumerateMetadataProperties()
    {
        // GetDocumentInfo is the lightweight structural check — unlike ReadMetadata it
        // must NOT dump the metadata dictionary. Guard against the two tools drifting
        // into the same payload.
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = SampleDocuments.AuthoredPdf },
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        Assert.Contains("\"fileFormat\"", body);
        Assert.DoesNotContain("\"properties\"", body);
    }

    [Fact]
    public async Task GetDocumentInfo_UnknownFile_ReturnsErrorOrAvailableFilesHint()
    {
        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = "does-not-exist.pdf" },
            });

        var body = ToolResponse.Text(response);
        _output.WriteLine(body);

        var reported = (response.IsError ?? false)
            || body.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || body.Contains("available", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Document-info lookup failed for", StringComparison.OrdinalIgnoreCase);

        Assert.True(reported,
            $"Expected an error / available-files hint for an unknown file. Response:\n{body}");
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
    public async Task GetDocumentInfo_RealSample_ReportsExpectedMimeType(string fileName, string expectedMimeType)
    {
        if (!File.Exists(Path.Combine(_fixture.StoragePath, fileName)))
        {
            _output.WriteLine($"Sample '{fileName}' not present in storage — skipping.");
            return;
        }

        var catalog = await ToolCatalog.LoadAsync(_fixture.Client);

        var response = await _fixture.Client.CallToolAsync(
            catalog.DocumentInfo.Name,
            new Dictionary<string, object?>
            {
                ["file"] = new Dictionary<string, object?> { ["filePath"] = fileName },
            });

        Assert.False(response.IsError ?? false,
            $"Tool reported an error reading '{fileName}': {ToolResponse.Text(response)}");

        var json = ToolResponse.Json(response);
        _output.WriteLine(json.ToString());

        Assert.Equal(expectedMimeType, json.GetProperty("mimeType").GetString());
    }
}
