"""Serve the isolated WebGL smoke build with Unity's Brotli headers."""

from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import argparse


ROOT = Path(__file__).resolve().parents[2] / "Builds" / "MirraCloudSmokeTest"
UI_WITHOUT_POINTER_LOCK = False


class Server(ThreadingHTTPServer):
    # Windows SO_REUSEADDR allows two processes to bind the same test port,
    # yielding intermittent empty responses from an older server.
    allow_reuse_address = False


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

    def do_GET(self):
        if UI_WITHOUT_POINTER_LOCK and self.path.split("?", 1)[0] in ("/", "/index.html"):
            # Diagnostic harness only: IAB may reject pointer lock on its embedded
            # root document. Keep UI/network testing possible without rewriting
            # the Unity build or pretending this validates FPS mouse control.
            shim = '<script>HTMLCanvasElement.prototype.requestPointerLock = function() {};' \
                   'console.warn("[Local UI harness] Pointer lock disabled; camera/input acceptance excluded");</script>'
            body = (ROOT / "index.html").read_text(encoding="utf-8").replace("<head>", "<head>" + shim).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(body)
            return
        super().do_GET()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", default="MirraCloudSmokeTest", choices=["MirraCloudSmokeTest", "MirraChatProbe", "MirraGameChatPilot"])
    parser.add_argument("--port", type=int, default=8766)
    parser.add_argument("--ui-without-pointer-lock", action="store_true",
                        help="Local UI/network harness only; excludes camera/input acceptance")
    args = parser.parse_args()
    ROOT = Path(__file__).resolve().parents[2] / "Builds" / args.build
    UI_WITHOUT_POINTER_LOCK = args.ui_without_pointer_lock
    if not (ROOT / "index.html").exists():
        raise SystemExit(f"Build not found: {ROOT}")
    try:
        server = Server(("127.0.0.1", args.port), Handler)
    except OSError as error:
        raise SystemExit(f"Cannot bind test port {args.port}; stop its previous server: {error}")
    print(f"{args.build}: http://127.0.0.1:{args.port}/", flush=True)
    with server:
        server.serve_forever()
