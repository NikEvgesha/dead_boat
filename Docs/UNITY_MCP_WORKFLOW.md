# Unity MCP workflow

Current project standard: Ivan Murzak Unity-MCP (`com.ivanmurzak.unity.mcp`) with Codex skills generated under `.agents/skills`.

## Connection

- Unity window: `AI Game Developer`.
- Codex MCP config: `.codex/config.toml`.
- Local MCP URL: `http://localhost:22348`.
- Unity package: `com.ivanmurzak.unity.mcp` pinned to `0.72.0`.
- Local tool CLI: `npx.cmd unity-mcp-cli` from PowerShell. Avoid plain `npx` on this machine because `npx.ps1` can be blocked by execution policy.
- OpenUPM is used only for `extensions.unity.playerprefsex`.

The old project-local `com.unity-bridge` package and `Tools/unity-bridge` wrapper are retired. Do not use port `7778` for Codex/Unity work.

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
- MCP responds on `http://localhost:22348`.
- `editor-application-get-state` reports `IsCompiling: false`.
- `scene-list-opened` returns the active Unity scene.
