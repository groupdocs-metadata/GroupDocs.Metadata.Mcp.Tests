# Run via Docker

The MCP server is published as a container image to two registries:

- `ghcr.io/groupdocs-metadata/metadata-net-mcp` — GitHub Container Registry (primary)
- `docker.io/groupdocs/metadata-net-mcp` — Docker Hub (mirror)

Each release is tagged with its version (`:26.4.3`) and `:latest`.

## Prerequisites

```bash
docker --version
# Docker 20.10+ is fine; any recent version works
```

## One-off run

```bash
docker run --rm -i \
  -v $(pwd)/documents:/data \
  ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3
```

- `--rm` — delete the container when it exits.
- `-i` — **required** — keeps stdin open so the MCP client can send JSON-RPC. Do NOT add `-t` (that would allocate a TTY and break the JSON stream).
- `-v $(pwd)/documents:/data` — mount the folder containing your files at `/data` inside the container.

The image sets `ENV GROUPDOCS_MCP_STORAGE_PATH=/data` and declares `VOLUME /data`, so the client tools see filenames relative to whatever you mount.

## Pinned version

Always pin the version tag in production configs — `:latest` floats:

```bash
docker pull ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3
```

## Smoke test

```bash
(
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"probe","version":"1"}}}'
  echo '{"jsonrpc":"2.0","method":"notifications/initialized"}'
  echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
  sleep 2
) | docker run --rm -i \
    -v $(pwd)/documents:/data \
    ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3 2>/dev/null
```

Expected: two JSON-RPC responses on stdout. The second includes `read_metadata`
and `remove_metadata` with their descriptions and input schemas.

## docker-compose

A reference compose file lives at [examples/docker-compose.yml](../examples/docker-compose.yml):

```yaml
services:
  groupdocs-metadata-mcp:
    image: ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3
    stdin_open: true
    tty: false
    environment:
      GROUPDOCS_MCP_STORAGE_PATH: /data
    volumes:
      - ./documents:/data
```

Run with:

```bash
docker compose up
```

Compose is useful for local development, but MCP clients like Claude Desktop / VS Code expect a process they can launch themselves over stdio — they don't typically connect to a compose service. For those clients, point the `command` at `docker run` directly.

## Using the image from MCP clients

### Claude Desktop

```json
{
  "mcpServers": {
    "groupdocs-metadata": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-v", "/absolute/path/to/documents:/data",
        "ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3"
      ]
    }
  }
}
```

### VS Code / GitHub Copilot

See [examples/vscode-mcp.json](../examples/vscode-mcp.json) for the `dnx` variant. For Docker, swap `command` to `docker` and `args` to the `run` invocation above.

## Providing a license

Mount the `.lic` file read-only and point the env var at the mount path:

```bash
docker run --rm -i \
  -v $(pwd)/documents:/data \
  -v $(pwd)/secrets/GroupDocs.Total.lic:/licenses/GroupDocs.Total.lic:ro \
  -e GROUPDOCS_LICENSE_PATH=/licenses/GroupDocs.Total.lic \
  ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3
```

Without the license, `remove_metadata` returns the evaluation-mode error
(see [01 — NuGet](01-install-from-nuget.md#license)).

## Verifying the image

```bash
# Inspect the image (entrypoint, env, user)
docker inspect ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3 \
  --format '{{json .Config}}' | jq

# Expected:
# - Entrypoint: ["dotnet", "GroupDocs.Metadata.Mcp.dll"]
# - Env contains: GROUPDOCS_MCP_STORAGE_PATH=/data
# - User: mcpuser (uid 1000)
```

The image runs as a non-root user (`mcpuser`, uid 1000). If your mount's host
uid/gid doesn't allow reads, either `chmod o+r` the files or pass
`--user $(id -u):$(id -g)` to the `docker run` invocation (requires files be
world-readable anyway, but gives better audit trail).

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Client says "server crashed" immediately | Passed `-t` along with `-i` | Remove `-t`. MCP needs a clean stdio pipe. |
| Tools see no files | Mount path / env var mismatch | Confirm you mounted to `/data` and didn't override `GROUPDOCS_MCP_STORAGE_PATH`. |
| Permission denied writing output | Host mount is read-only or uid mismatch | Make the mount writable. `-v ./documents:/data` (not `:ro`). |
| `manifest unknown` / can't pull image | Version tag doesn't exist on that registry | Check [ghcr.io/groupdocs-metadata/metadata-net-mcp](https://github.com/groupdocs-metadata/GroupDocs.Metadata.Mcp/pkgs/container/metadata-net-mcp) for available tags. |

## Next steps

- [05 — Use with VS Code / Copilot](05-use-with-vscode-copilot.md) — Docker launcher config
- [03 — MCP registry](03-verify-mcp-registry.md) — confirm the container is listed correctly
- [06 — Integration tests](06-run-integration-tests.md) — exercise the image end-to-end
