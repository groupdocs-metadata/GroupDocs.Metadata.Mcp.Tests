---
id: 001
date: 2026-04-23
package-under-test: 26.4.3
type: feature
---

# Initial integration test suite for GroupDocs.Metadata.Mcp

## What changed

- xUnit test project targeting `net10.0`, referencing only the published
  `ModelContextProtocol` 1.1.0 NuGet — no project reference to the server source.
- `McpServerFixture` launches the published `GroupDocs.Metadata.Mcp@26.4.3`
  package via `dnx` as a child process, wires an MCP stdio client, and seeds a
  temporary storage folder with synthetic sample documents.
- `SampleDocuments` builds a minimal valid PDF (with Info dictionary → Author /
  Title) and a baseline JPEG from byte arrays at runtime — no binary files in repo.
- Four test classes, 12 tests total:
  - `ToolDiscoveryTests` — server info, `tools/list`, input schema validation.
  - `ReadMetadataTests` — PDF + JPEG format/mime/properties, known-author check.
  - `RemoveMetadataTests` — branches on `GROUPDOCS_LICENSE_PATH`; asserts graceful
    failure in evaluation mode and clean-output round-trip when licensed.
  - `ErrorHandlingTests` — unknown file, corrupted bytes, password parameter.
- GitHub Actions workflow `.github/workflows/integration.yml`:
  - Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.
  - Triggers: push, PR, nightly cron, `workflow_dispatch` (with `package_version`
    input), `repository_dispatch` (`nuget-published` event for release smoke).
  - Optional `GROUPDOCS_LICENSE` repo secret auto-decoded into `$RUNNER_TEMP` and
    exported as `GROUPDOCS_LICENSE_PATH` to unlock licensed-mode tests.
- `examples/` — ready-to-use `claude-desktop.json`, `vscode-mcp.json`,
  `docker-compose.yml` copy-paste configs.
- `AGENTS.md` + `llms.txt` for AI coding agent orientation.
- `how-to/` guides covering every deployment channel (NuGet via dnx / dotnet
  tool, Docker, MCP registry, Claude Desktop, VS Code / GitHub Copilot, plus
  running this test suite).

## Why

Closes the release-validation gap: the main repo's unit tests mock
`IFileResolver` / `ILicenseManager` and validate tool logic, but nothing
previously exercised the **shipped** NuGet end-to-end. Every release now has a
cross-platform smoke check against live nuget.org before users hit it.

## Migration / impact

First release of this repository — no migration. To wire the release-smoke
trigger, add a `gh api repos/.../dispatches -f event_type=nuget-published -f
'client_payload[package_version]=…'` step to the main repo's publish workflow
after `dotnet nuget push` succeeds. See `how-to/06-run-integration-tests.md`.
