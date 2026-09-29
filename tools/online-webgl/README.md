# WebGL smoke test support

`jquery-3.1.0.min.js` is copied from the jQuery Foundation distribution referenced by Mirra Cloud SDK v0.10.0's bundled UnityWebView WebGL template. Its license is in `jquery-LICENSE.txt`.

The separate Mirra Cloud smoke build copies this local file and the SDK's `unity-webview-2020/unity-webview.js` into the build, then loads both before `unityApp.js`. Without them, SDK initialization stops WebGL with `unityWebView is not defined`, even when testing only guest login. This does not modify the normal game build; its template still needs a production integration decision.

After building, run `python tools/online-webgl/serve_smoke.py` from the repository root and open `http://127.0.0.1:8766/`. The helper serves Unity's `.unityweb` files with Brotli headers. It listens only on loopback and does not store any credentials.
