# Unity MCP workflow

Current project standard: Ivan Murzak Unity-MCP (`com.ivanmurzak.unity.mcp`) with Codex skills generated under `.agents/skills`.

## Connection

- Unity window: `AI Game Developer`.
- Codex MCP config: `.codex/config.toml`.
- Connection mode: `Custom`.
- Transport: `streamableHttp`.
- Local MCP URL / Server URL: `http://localhost:22348`.
- Unity package: `com.ivanmurzak.unity.mcp` pinned to `0.86.0`.
- Installed MCP extensions:
  - `com.ivanmurzak.unity.mcp.animation` `1.2.28` (OpenUPM registry);
  - `com.ivanmurzak.unity.mcp.particlesystem` `1.2.28` (OpenUPM registry);
  - `com.ivanmurzak.unity.mcp.probuilder` `1.2.28` (project-embedded package).
- Local tool CLI: `npx.cmd unity-mcp-cli` from PowerShell. Avoid plain `npx` on this machine because `npx.ps1` can be blocked by execution policy.
- OpenUPM provides Unity-MCP, its registry-backed extensions, and their scoped dependencies.

The old project-local `com.unity-bridge` package, `Tools/unity-bridge` wrapper, and Codex MCP server `unity-mcp-dead-boat` are retired. Do not use ports `7777`/`7778` for Codex/Unity work.

Global Codex config must not enable the old Node bridge:

```toml
[mcp_servers.ai-game-developer]
enabled = true
startup_timeout_sec = 30
tool_timeout_sec = 300
url = "http://localhost:22348"

[mcp_servers."unity-mcp-dead-boat"]
enabled = false
```

## Quick checks

From the project root:

```powershell
npx.cmd unity-mcp-cli run-tool tool-list --input "{}"
```

```powershell
npx.cmd unity-mcp-cli run-tool editor-application-get-state --input "{}"
```

```powershell
npx.cmd unity-mcp-cli run-tool scene-list-opened --input "{}"
```

```powershell
npx.cmd unity-mcp-cli run-tool console-get-logs --input "{`"logType`":`"Error`",`"maxEntries`":50}"
```

If inline JSON is fragile in PowerShell, write UTF-8 without BOM and pass `--input-file`:

```powershell
$inputPath = Join-Path $env:TEMP 'unity-mcp-input.json'
[System.IO.File]::WriteAllText($inputPath, '{"logType":"Error","maxEntries":50}', (New-Object System.Text.UTF8Encoding($false)))
npx.cmd unity-mcp-cli run-tool console-get-logs --input-file $inputPath
```

## Expected state

- Unity is open on this project.
- `AI Game Developer` is configured for Codex.
- AI Game Developer shows `Unity: Connected` for `http://localhost:22348`.
- `editor-application-get-state` reports `IsCompiling: false`.
- `scene-list-opened` returns the active Unity scene.
- `package-list` reports Unity-MCP `0.86.0` and all three installed extensions at `1.2.28`.
