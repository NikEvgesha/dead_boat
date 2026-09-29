"""Serve the isolated WebGL smoke build with Unity's Brotli headers."""

from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2] / "Builds" / "MirraCloudSmokeTest"


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT), **kwargs)

    def end_headers(self):
        if self.path.split("?", 1)[0].endswith(".unityweb"):
            self.send_header("Content-Encoding", "br")
            if ".wasm." in self.path:
                self.send_header("Content-Type", "application/wasm")
            elif ".js." in self.path:
                self.send_header("Content-Type", "application/javascript")
            else:
                self.send_header("Content-Type", "application/octet-stream")
        super().end_headers()


if __name__ == "__main__":
    if not (ROOT / "index.html").exists():
        raise SystemExit(f"Build not found: {ROOT}")
    print("Mirra Cloud smoke test: http://127.0.0.1:8766/", flush=True)
    ThreadingHTTPServer(("127.0.0.1", 8766), Handler).serve_forever()
