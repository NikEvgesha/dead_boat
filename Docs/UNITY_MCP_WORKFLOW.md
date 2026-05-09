# Unity MCP workflow

Current project standard: Ivan Murzak Unity-MCP (`com.ivanmurzak.unity.mcp`) with Codex skills generated under `.agents/skills`.

## Connection

- Unity window: `AI Game Developer`.
- Codex MCP config: `.codex/config.toml`.
- Local MCP URL: `http://localhost:22348`.
- Unity package: `com.ivanmurzak.unity.mcp` pinned to `0.71.0` from GitHub.
- OpenUPM is used only for `extensions.unity.playerprefsex`.

The old project-local `com.unity-bridge` package and `Tools/unity-bridge` wrapper are retired. Do not use port `7778` for Codex/Unity work.

## Quick checks

From the project root:

```powershell
npx unity-mcp-cli status E:\GitFork\dead_boat --timeout 15000 --verbose
```

```powershell
npx unity-mcp-cli run-tool editor-application-get-state E:\GitFork\dead_boat --input "{}" --timeout 30000 --raw
```

```powershell
npx unity-mcp-cli run-tool scene-list-opened E:\GitFork\dead_boat --input "{}" --timeout 30000 --raw
```

```powershell
npx unity-mcp-cli run-tool tool-list E:\GitFork\dead_boat --input "{}" --timeout 30000 --raw
```

## Expected state

- Unity is open on this project.
- `AI Game Developer` is configured for Codex.
- MCP responds on `http://localhost:22348`.
- `editor-application-get-state` reports `IsCompiling: false`.
- `scene-list-opened` returns the active Unity scene.
