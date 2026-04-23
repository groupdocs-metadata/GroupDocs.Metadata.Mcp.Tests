# Use with Claude Desktop

Connect the MCP server to Claude Desktop (macOS / Windows) so you can ask
Claude to read or remove metadata from your documents.

## Prerequisites

- [Claude Desktop](https://claude.ai/download) installed and logged in.
- One of:
  - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for the `dnx` route — recommended), or
  - [Docker](https://www.docker.com/products/docker-desktop) (for the container route).

## Config file location

| OS | Path |
|---|---|
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |

Create the file if it doesn't exist.

## Option A — dnx (recommended)

```json
{
  "mcpServers": {
    "groupdocs-metadata": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Metadata.Mcp@26.4.3", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents"
      }
    }
  }
}
```

- Replace `/Users/you/Documents` with an **absolute path** to the folder
  containing documents you want Claude to operate on.
- On Windows use `"C:\\Users\\you\\Documents"` (double-escaped backslashes) or
  forward slashes: `"C:/Users/you/Documents"`.

Full example: [examples/claude-desktop.json](../examples/claude-desktop.json).

### If Claude can't find `dnx`

Claude Desktop launches child processes with a minimal PATH — `dnx` may not be
found on macOS even though it works in your shell. Use the absolute path:

```json
"command": "/usr/local/share/dotnet/dnx"
```

On Windows:

```json
"command": "C:\\Program Files\\dotnet\\dnx.cmd"
```

Find the correct path with:

```bash
which dnx            # macOS / Linux
where dnx.cmd        # Windows (from cmd)
```

## Option B — Docker

```json
{
  "mcpServers": {
    "groupdocs-metadata": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "-v", "/Users/you/Documents:/data",
        "ghcr.io/groupdocs-metadata/metadata-net-mcp:26.4.3"
      ]
    }
  }
}
```

This works even if you don't have the .NET SDK installed. The first invocation
pulls the image; subsequent launches are fast.

## Option C — Global dotnet tool

```json
{
  "mcpServers": {
    "groupdocs-metadata": {
      "type": "stdio",
      "command": "groupdocs-metadata-mcp",
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents"
      }
    }
  }
}
```

Requires you've already run `dotnet tool install -g GroupDocs.Metadata.Mcp`
(see [01 — NuGet install](01-install-from-nuget.md)).

## Restart Claude Desktop

After editing the config, fully quit and reopen Claude Desktop. On macOS,
`Cmd+Q` — closing the window isn't enough.

## Verify the connection

1. Open a new conversation.
2. Click the **🔨 tools** icon in the composer — you should see
   `read_metadata` and `remove_metadata` listed under `groupdocs-metadata`.
3. If the icon shows an error badge, hover for the details. The most common
   issue is a bad `command` path or invalid `GROUPDOCS_MCP_STORAGE_PATH`.

## Example prompts

```
Read the metadata from report.pdf.

What's the author and creation date of contract.docx?

Remove all metadata from every file in the /uploads folder.

Does sample.jpg have GPS coordinates in its EXIF data?

Summarize the metadata differences between old.pdf and new.pdf.
```

Claude will call `read_metadata` (and `remove_metadata` where relevant) and
compose its answer from the tool results.

## License note

`remove_metadata` requires a GroupDocs license. Without one, the tool returns
an error. Add the license path to your config:

```json
"env": {
  "GROUPDOCS_MCP_STORAGE_PATH": "/Users/you/Documents",
  "GROUPDOCS_LICENSE_PATH": "/Users/you/.secrets/GroupDocs.Total.lic"
}
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| Server not listed in tools icon | Config JSON has a typo — Claude silently drops unparseable entries. Run it through `jq . claude_desktop_config.json`. |
| Server listed but greyed out | Claude couldn't launch the process. Check `~/Library/Logs/Claude/mcp*.log` on macOS or `%APPDATA%\Claude\logs\mcp*.log` on Windows for stderr from the server. |
| "No license configured" warnings | Expected in evaluation mode. `read_metadata` still works. |
| `Could not save the file. Evaluation only.` | `remove_metadata` without license. Set `GROUPDOCS_LICENSE_PATH`. |

## Next steps

- [05 — Use with VS Code / Copilot](05-use-with-vscode-copilot.md)
- [03 — MCP registry](03-verify-mcp-registry.md) — confirm the snippet matches what's on nuget.org
