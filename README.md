# GroupDocs.Metadata.Mcp.Tests

Integration tests for the [`GroupDocs.Metadata.Mcp`](https://www.nuget.org/packages/GroupDocs.Metadata.Mcp)
NuGet package — an MCP server that exposes
[GroupDocs.Metadata](https://products.groupdocs.com/metadata) as AI-callable tools.

This repository validates the **published** NuGet artifact end-to-end: it
launches the server via `dnx`, connects as an MCP client, and exercises every
advertised tool. It also doubles as a copy-pasteable set of example configs
and user-facing how-to guides for every deployment channel.

## Documentation

- [how-to/](how-to/) — step-by-step guides for every deployment channel
  ([NuGet](how-to/01-install-from-nuget.md),
  [Docker](how-to/02-run-via-docker.md),
  [MCP registry](how-to/03-verify-mcp-registry.md),
  [Claude Desktop](how-to/04-use-with-claude-desktop.md),
  [VS Code / Copilot](how-to/05-use-with-vscode-copilot.md),
  [running the tests](how-to/06-run-integration-tests.md)).
- [examples/](examples/) — ready-to-paste `claude-desktop.json`,
  `vscode-mcp.json`, and `docker-compose.yml`.
- [AGENTS.md](AGENTS.md) — orientation for AI coding agents working in this repo.
- [llms.txt](llms.txt) — machine-readable summary for LLM tooling.
- [changelog/](changelog/) — one entry per change set (see
  [changelog/README.md](changelog/README.md) for format).

## What gets tested

| Area | Covered by |
|---|---|
| Package installs and starts via `dnx` | [McpServerFixture](src/GroupDocs.Metadata.Mcp.Tests/Fixtures/McpServerFixture.cs) |
| MCP handshake, server info, version | [ToolDiscoveryTests](src/GroupDocs.Metadata.Mcp.Tests/ToolDiscoveryTests.cs) |
| `ReadMetadata` — PDF + JPEG, schema + values | [ReadMetadataTests](src/GroupDocs.Metadata.Mcp.Tests/ReadMetadataTests.cs) |
| `RemoveMetadata` — output file + read-back check | [RemoveMetadataTests](src/GroupDocs.Metadata.Mcp.Tests/RemoveMetadataTests.cs) |
| Unknown / corrupted files, password parameter | [ErrorHandlingTests](src/GroupDocs.Metadata.Mcp.Tests/ErrorHandlingTests.cs) |

## Running locally

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet test
```

Test a specific published version:

```bash
dotnet test -p:McpPackageVersion=26.4.3
# or
MCP_PACKAGE_VERSION=26.4.3 dotnet test
```

The first run downloads the NuGet package — subsequent runs are cached.

## CI

[.github/workflows/integration.yml](.github/workflows/integration.yml) runs on:

- Every push / PR.
- Nightly cron — catches regressions in nuget.org, the dnx shim, or the .NET runtime.
- `workflow_dispatch` with a `package_version` input — manual smoke of any version.
- `repository_dispatch` (`nuget-published`) — fires from the main repo's publish pipeline
  so every release is smoke-tested against live nuget.org. See
  [Release smoke hook](#release-smoke-hook).

Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.

## Release smoke hook

To auto-verify each release, add this step to the main repo's publish workflow
after the `dotnet nuget push` step:

```yaml
- name: Dispatch smoke tests
  env:
    GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
  run: |
    gh api repos/groupdocs-metadata/GroupDocs.Metadata.Mcp.Tests/dispatches \
      -f event_type=nuget-published \
      -f 'client_payload[package_version]=${{ steps.version.outputs.version }}'
```

## Evaluation vs licensed mode

GroupDocs.Metadata refuses to `Save()` any file in evaluation mode — so
`RemoveMetadata` always returns an error unless a license is configured.
Tests branch on `GROUPDOCS_LICENSE_PATH`:

- **Unset (default):** `RemoveMetadata_InEvaluationMode_ReturnsErrorResponse`
  runs and verifies the tool fails gracefully. Licensed-mode tests no-op.
- **Set:** Licensed tests run — they assert the cleaned file is produced and
  is re-readable by `ReadMetadata`.

For CI, store a base64-encoded `.lic` file as repo secret `GROUPDOCS_LICENSE`
— the workflow decodes it into `$RUNNER_TEMP` and exports
`GROUPDOCS_LICENSE_PATH` automatically.

## Fixture documents

Sample documents are built from byte-arrays in
[SampleDocuments.cs](src/GroupDocs.Metadata.Mcp.Tests/Fixtures/SampleDocuments.cs)
at test startup — no binary files are checked into this repo.

To add a real-world fixture, drop it into [sample-docs/](sample-docs/) and write
a test that references it by filename. The project auto-copies everything in
`sample-docs/` to the test output.

## Using this repo as a starter

Copy configs from [examples/](examples/):

- [claude-desktop.json](examples/claude-desktop.json) — Claude Desktop MCP server config.
- [vscode-mcp.json](examples/vscode-mcp.json) — VS Code / GitHub Copilot.
- [docker-compose.yml](examples/docker-compose.yml) — containerized deployment.

## License

MIT — see [LICENSE](LICENSE).
