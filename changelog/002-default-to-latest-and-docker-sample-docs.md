---
id: 002
date: 2026-04-27
package-under-test: latest
type: feature
---

# Track latest by default + new sample-docs Docker runner

## What changed

### How-to docs
- [how-to/01-install-from-nuget.md](how-to/01-install-from-nuget.md) — added a "Pinned vs always-latest" sub-section under Option 1 (`dnx`): both unpinned forms (`dnx GroupDocs.Metadata.Mcp --yes` and `--prerelease --yes`), a comparison table covering reproducibility / breakage risk / day-of-release startup time, the lack of npm-style ranges in `dnx`, and the cache location for pruning old versions.
- [how-to/02-run-via-docker.md](how-to/02-run-via-docker.md):
  - Prerequisites now mention the Docker Desktop daemon requirement on Windows / macOS (with the exact `failed to connect to the docker API at npipe://...` symptom) and the WSL2 vs Hyper-V file-sharing distinction on Windows.
  - "Pinned version" replaced with a fuller "Pinned vs always-latest" section — both `:26.4.3` and `:latest` forms, the **Docker cache gotcha** (Docker is sticky; `:latest` won't auto-refresh without `docker pull` or `--pull always`), comparison table, and a new "Verifying which version `:latest` resolved to" snippet using the `org.opencontainers.image.version` OCI label.
  - Troubleshooting gained two rows: "daemon not running" and "`:latest` ran but didn't pick up a new release".

### docker-scripts — default to `latest`
- [docker-scripts/02_test-all-scenarios.sh](docker-scripts/02_test-all-scenarios.sh) and [docker-scripts/03_test-docker-compose.sh](docker-scripts/03_test-docker-compose.sh) — `MCP_PACKAGE_VERSION` default flipped from `26.4.3` to `latest`. Help text + examples updated to lead with the unpinned form and show pinning as the alternative for reproducible / CI runs.
- [docker-scripts/00_quick-start.sh](docker-scripts/00_quick-start.sh) and [docker-scripts/README.md](docker-scripts/README.md) — example commands and tables synced with the new default.

### docker-scripts — new `04_run-server-with-samples.sh`
- Fills a real gap: the existing `02/03` scripts only exercise the NuGet+`dnx` path; **none of them launch the published `ghcr.io/groupdocs-metadata/metadata-net-mcp` Docker image**. The new script does, with `sample-docs/` mounted at `/data:ro` so the bundled fixtures (`sample.pdf`, `sample.jpg`, `sample.docx`, `sample.xlsx`, `sample.png`) are immediately addressable.
- Two modes: **interactive** (default — server hangs waiting for stdin) and `--smoke` (pipes `initialize` + `tools/list` through stdin and asserts both `read_metadata` and `remove_metadata` are advertised; CI-friendly, exits non-zero on failure).
- `--pull always` by default (probes registry on every launch); `--no-pull` to use the cached image.
- Optional `--license PATH` to mount a GroupDocs license and unlock licensed-mode `remove_metadata`.
- Configurable `--image-tag` for pinned runs.
- [docker-scripts/01_verify-setup.sh](docker-scripts/01_verify-setup.sh) — adds `04_run-server-with-samples.sh` to the script-presence check.

### Test fixture — handle `latest` end-to-end
- [src/GroupDocs.Metadata.Mcp.Tests/Fixtures/PackageVersion.cs](src/GroupDocs.Metadata.Mcp.Tests/Fixtures/PackageVersion.cs) — fallback default flipped from `"26.4.3"` to `"latest"`. New `IsLatest` property (true for `"latest"` or empty/whitespace).
- [src/GroupDocs.Metadata.Mcp.Tests/Fixtures/McpServerFixture.cs](src/GroupDocs.Metadata.Mcp.Tests/Fixtures/McpServerFixture.cs) — when `IsLatest`, builds `dnx GroupDocs.Metadata.Mcp --yes` (no `@`). `dnx` has no `@latest` literal — to get the latest stable, the `@<version>` part must be omitted entirely.

## Why

Two motivations:

**Tracking latest** — users who upgrade the MCP package on a 1–2 month cadence shouldn't have to find-and-replace a version pin every time. The how-to docs and scripts now lead with the unpinned form (mirrors how `npx` / `dnx` are typically used) and show pinning as the deliberate choice for shared / CI configs. The `latest` keyword flows cleanly through bash → MSBuild prop → env var → `PackageVersion` → fixture without breaking on the `@latest` literal that `dnx` doesn't accept.

**Coverage gap on the Docker artifact** — the published `metadata-net-mcp` container image had **no test path** in this repo. The `02/03` scripts test the NuGet artifact (via `dnx`) from the host or a `dotnet/sdk` container; neither launches the production image. `04_run-server-with-samples.sh` closes that gap and is also the only `latest`-tracking path that works on Linux today (until the SkiaSharp Linux native asset fix lands in the next NuGet release, the `dnx` flow stays broken on Ubuntu CI — the Docker image works because the Debian-based runtime base ships system libs SkiaSharp falls back to).

## Migration / impact

- **Default behaviour change:** running `02_test-all-scenarios.sh` / `03_test-docker-compose.sh` with no `--version` now hits the latest stable instead of `26.4.3`. To preserve the prior behaviour, pass `--version 26.4.3` explicitly, or set `MCP_PACKAGE_VERSION=26.4.3` in the environment.
- **CI consumers** of these scripts should pin (`--version <release>`) to avoid surprise upgrades — the bash help text and README emphasise this.
- **`04_run-server-with-samples.sh`** is new — no migration needed, but worth surfacing in CI as a Docker-channel smoke check (`./04_run-server-with-samples.sh --smoke` returns exit 0 / non-zero).
