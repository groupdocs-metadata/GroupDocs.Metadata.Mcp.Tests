---
id: 003
date: 2026-07-10
package-under-test: 26.7.1
type: feature
---

# Target 26.7.1 — full tool coverage (get_document_info, search, write, selective remove) + eval-mode pivot

Brings the suite from the 2-tool 26.5.1 baseline up to the 5-tool 26.7.x server. The server shipped
these across two releases — 26.7.0 (`get_document_info` + descriptive error contract) and 26.7.1
(`search_metadata`, `write_metadata`, selective `remove_metadata`) — but the tests land in one
batch pinned to the latest published version, **26.7.1**.

## What changed
- **Default package-under-test `26.5.1` → `26.7.1`** in `Directory.Build.props` and
  `.github/workflows/integration.yml` (both the `workflow_dispatch` input default and the
  `MCP_PACKAGE_VERSION` env fallback).
- **Coverage for the 5-tool surface:**
  - `GetDocumentInfoTests` — format / page count / MIME type; asserts no metadata dump.
  - `SearchMetadataTests` — name/category/value filters + zero-match (read-only, runs in eval).
  - `WriteMetadataTests` — unknown-property error, eval-mode `Save()` failure, licensed round-trip.
  - `RemoveMetadataTests` — added selective `categories` cases (eval failure + licensed author-only).
  - `ToolDiscoveryTests` count assertion `2 → 5`; `ToolCatalog` gains `DocumentInfo` / `Search` / `Write` resolvers.
- **Eval-mode assertion pivoted for the 26.7.0 error contract.** The server now catches the
  engine exception and returns a descriptive string (`IsError` is false), so
  `RemoveMetadata_InEvaluationMode_ReturnsDescriptiveFailure` (renamed from `…ReturnsErrorResponse`)
  asserts the body contains `"Metadata removal failed for"` + `"Evaluation only"` rather than
  `IsError == true`. Same pivot applies to the new write / selective-remove eval cases.
- **Doc/version-pin refresh** to `26.7.1` across `how-to/*`, `README.md`, `AGENTS.md`,
  `examples/*` (docker-compose pinned off `:latest`), `docker-scripts/*`, and the
  `changelog/README.md` template example.
- Removed a stray empty `sample-docs;C/` directory (shell artifact from an earlier session).

## Migration / impact
- Full integration validation runs on CI once `GroupDocs.Metadata.Mcp@26.7.1` is published to
  NuGet.org (the suite pulls the server via `dnx` at runtime). Locally the project only compiles
  until the package is live. The write / selective-remove licensed paths need `GROUPDOCS_LICENSE_PATH`;
  everything else runs in evaluation mode.
