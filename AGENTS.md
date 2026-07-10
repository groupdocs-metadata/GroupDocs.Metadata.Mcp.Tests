# AGENTS.md — Guide for AI coding agents

Brief orientation for AI coding agents (Claude Code, Copilot, Cursor, Aider, Amp, Codex) working in this repository.

## What this repo is

**Integration tests** for the [`GroupDocs.Metadata.Mcp`](https://www.nuget.org/packages/GroupDocs.Metadata.Mcp) NuGet package — an MCP server that exposes GroupDocs.Metadata for .NET as AI-callable tools.

This repo is **not** the server itself. The server lives at [groupdocs-metadata/GroupDocs.Metadata.Mcp](https://github.com/groupdocs-metadata/GroupDocs.Metadata.Mcp). This repo:

1. Consumes only the **published** NuGet artifact (no project references).
2. Launches the server via `dnx`, connects as an MCP stdio client, and exercises every advertised tool.
3. Doubles as a copy-pasteable set of example configs and how-to guides for all deployment channels (NuGet, Docker, MCP registry, Claude Desktop, VS Code).

## Folder layout

```
src/GroupDocs.Metadata.Mcp.Tests/
  Fixtures/
    McpServerFixture.cs          ← launches dnx child process, wires stdio MCP client
    SampleDocuments.cs           ← builds minimal PDF + JPEG from byte arrays at runtime
    ToolCatalog.cs               ← keyword-based tool name resolution (read/search/write/remove/document_info)
    ToolResponse.cs              ← CallToolResult text/JSON extraction
    CommandResolver.cs           ← cross-platform dnx.cmd resolution on Windows
    PackageVersion.cs            ← pulls version from env / assembly metadata / default
  ToolDiscoveryTests.cs          ← handshake, tools/list, schema validation
  ReadMetadataTests.cs           ← PDF + JPEG happy-path + known-value assertions
  SearchMetadataTests.cs         ← name/category/value filters, zero-match (read-only, eval-safe)
  WriteMetadataTests.cs          ← unknown-property + eval-mode failure + licensed round-trip
  RemoveMetadataTests.cs         ← full + selective categories; branches on GROUPDOCS_LICENSE_PATH
  GetDocumentInfoTests.cs        ← format / page count / MIME type; asserts no metadata dump
  ErrorHandlingTests.cs          ← unknown file, corrupted bytes, password parameter
  GroupDocs.Metadata.Mcp.Tests.csproj
.github/workflows/integration.yml  ← matrix × 3 OS, nightly cron, release-smoke dispatch
changelog/                         ← one MD file per change (NNN-slug.md)
how-to/                            ← user-facing guides for every deployment channel
examples/                          ← claude-desktop.json, vscode-mcp.json, docker-compose.yml
sample-docs/                       ← drop real fixture files here; copied to test output
Directory.Build.props              ← McpPackageVersion property (overridable)
global.json                        ← pinned to .NET 10.0.100
```

## What gets tested

| Area | Covered by |
|---|---|
| Package installs and starts via `dnx` | `McpServerFixture` |
| MCP handshake, server info, version | `ToolDiscoveryTests` |
| `read_metadata` — PDF + JPEG, schema + values | `ReadMetadataTests` |
| `search_metadata` — name/category/value filters, zero-match | `SearchMetadataTests` |
| `write_metadata` — unknown-property, eval failure, licensed round-trip | `WriteMetadataTests` |
| `remove_metadata` — full + selective `categories`, read-back (licensed) | `RemoveMetadataTests` |
| `get_document_info` — format / page count / MIME type, no metadata dump | `GetDocumentInfoTests` |
| Unknown / corrupted files, password parameter | `ErrorHandlingTests` |

## Commands you can run

```bash
# Restore + build
dotnet restore
dotnet build -c Release

# Run the full suite against the default package version (26.7.1)
dotnet test -c Release

# Run against a specific published version
dotnet test -c Release -p:McpPackageVersion=26.7.1
# or
MCP_PACKAGE_VERSION=26.7.1 dotnet test -c Release

# Unlock licensed-mode RemoveMetadata tests
GROUPDOCS_LICENSE_PATH=/path/to/GroupDocs.Total.lic dotnet test -c Release

# Run just the discovery suite (fastest — no tool invocations)
dotnet test -c Release --filter "FullyQualifiedName~ToolDiscovery"
```

## Key design decisions

1. **Keyword-based tool resolution.** `ToolCatalog.Resolve("read")` picks the tool whose name contains "read" (case-insensitive). The MCP C# SDK converts `[McpServerTool]` method names to `snake_case` — so the actual wire names are `read_metadata`, `search_metadata`, `write_metadata`, `remove_metadata`, and `get_document_info`, not `ReadMetadata`. Keywords must be snake_case substrings of the wire name (e.g. `document_info`, not `documentinfo` — `Contains` is literal). Tests stay robust if that convention changes.

2. **Synthetic fixtures.** `SampleDocuments.cs` builds a minimal valid PDF (with Info dict → Author/Title) and a valid baseline JPEG from byte arrays. No binary files in the repo. To add real-world fixtures, drop them in `sample-docs/` — the csproj auto-copies everything there to the test output.

3. **Evaluation-mode branching.** `GroupDocs.Metadata.Save()` throws `"Could not save the file. Evaluation only."` when no license is configured. As of MCP 26.7.1 the server catches that exception and returns a descriptive `"Metadata removal failed for … Evaluation only"` string (it no longer flips `IsError`), so `RemoveMetadata_InEvaluationMode_ReturnsDescriptiveFailure` asserts on the response text, not `IsError`. Licensed tests (`*_Licensed` suffix) no-op unless `GROUPDOCS_LICENSE_PATH` is set. CI auto-decodes a `GROUPDOCS_LICENSE` repo secret into `$RUNNER_TEMP`.

4. **Responses are valid JSON.** As of MCP 26.5.1, `ReadMetadata` / `GetDocumentInfo` return raw `JsonSerializer.Serialize(...)` output directly (no `OutputHelper.TruncateText`), so responses are always well-formed JSON. `GetDocumentInfo` payloads are tiny and parse strictly; large `ReadMetadata` PDF/OOXML responses still use substring checks defensively rather than `JsonDocument.Parse`.

5. **No project references to the server.** The csproj only references `ModelContextProtocol` 1.1.0. If the server source breaks in the sibling repo, these tests still pass — they validate the shipped NuGet artifact.

## House rules

1. **Changelog entries required** — any PR that changes behaviour adds `changelog/NNN-slug.md` (schema in `changelog/README.md`).
2. **How-to guides track deployment reality** — if the main repo publishes a new channel (e.g. new Docker registry), add a guide under `how-to/` *and* update `README.md`.
3. **Version bumps flow through `Directory.Build.props`** — `<McpPackageVersion>` is the single source of truth for "what version are we testing." CI overrides it via env var / workflow input.
4. **Tests must not require the main repo's source.** If a test needs a server-side change, file an issue there — don't work around it here.
5. **Target framework is `net10.0` only** — required by `dnx` and the MCP SDK.

## Release smoke hook

The main repo's `publish_prod.yml` should fire a `repository_dispatch` with `event_type=nuget-published` after `dotnet nuget push` succeeds. The workflow in `.github/workflows/integration.yml` consumes `client_payload.package_version` and runs the matrix against the just-published version. This closes the loop: publish → smoke-test live nuget.org → fail loud if broken.

## What NOT to change

- Do not add a `ProjectReference` to the main repo's `GroupDocs.Metadata.Mcp.csproj`. This repo exists to test the shipped NuGet, not the source.
- Do not hardcode tool names as string literals (`"read_metadata"`). Use the `ToolCatalog` resolvers: `Read.Name` / `Search.Name` / `Write.Name` / `Remove.Name` / `DocumentInfo.Name`.
- Do not commit real license files or binary fixtures with unclear provenance. License goes through the `GROUPDOCS_LICENSE` CI secret; fixtures in `sample-docs/` must be self-authored or CC0/Apache-2.0.
