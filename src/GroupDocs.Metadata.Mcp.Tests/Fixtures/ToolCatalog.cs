using ModelContextProtocol.Client;

namespace GroupDocs.Metadata.Mcp.IntegrationTests.Fixtures;

/// Resolves tool names by keyword. The server-side attribute [McpServerTool] uses
/// the method name verbatim today (PascalCase: ReadMetadata, RemoveMetadata,
/// GetDocumentInfo, SearchMetadata, WriteMetadata → wire names read_metadata,
/// remove_metadata, get_document_info, search_metadata, write_metadata), but
/// keyword-based resolution keeps tests robust against future renames / casing
/// convention changes. NOTE: keywords must be snake_case substrings of the wire name
/// (Contains is literal — "get_document_info".Contains("documentinfo") is false).
internal sealed class ToolCatalog
{
    private readonly IReadOnlyList<McpClientTool> _tools;

    private ToolCatalog(IReadOnlyList<McpClientTool> tools) => _tools = tools;

    public static async Task<ToolCatalog> LoadAsync(McpClient client, CancellationToken ct = default)
    {
        var tools = await client.ListToolsAsync(cancellationToken: ct);
        return new ToolCatalog(tools.ToList());
    }

    public IReadOnlyList<McpClientTool> All => _tools;

    public McpClientTool Read => Resolve("read");
    public McpClientTool Remove => Resolve("remove");
    public McpClientTool DocumentInfo => Resolve("document_info");
    public McpClientTool Search => Resolve("search");
    public McpClientTool Write => Resolve("write");

    private McpClientTool Resolve(string keyword) =>
        _tools.FirstOrDefault(t => t.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No tool with name containing '{keyword}'. Found: {string.Join(", ", _tools.Select(t => t.Name))}");
}
