# WebGL smoke test support

`jquery-3.1.0.min.js` is copied from the jQuery Foundation distribution referenced by Mirra Cloud SDK v0.10.0's bundled UnityWebView WebGL template. Its license is in `jquery-LICENSE.txt`.

The separate Mirra Cloud smoke build copies this local file and the SDK's `unity-webview-2020/unity-webview.js` into the build, then loads both before `unityApp.js`. Without them, SDK initialization stops WebGL with `unityWebView is not defined`, even when testing only guest login. This does not modify the normal game build; its template still needs a production integration decision.

After building, run `python tools/online-webgl/serve_smoke.py` from the repository root and open `http://127.0.0.1:8766/`. The helper serves Unity's `.unityweb` files with Brotli headers. It listens only on loopback and does not store any credentials.

## Isolated chat probe

Queue `DeadBoat.Online.SmokeTest.Editor.MirraCloudSmokeBuild.QueueChatProbe()` from Unity tooling. It creates a saved, separate scene and builds only that scene to `Builds/MirraChatProbe`; normal game builds exclude the diagnostic component via scripting defines. The build rejects a nonempty Cloud API token, like the other smoke builds.

Serve with `python tools/online-webgl/serve_smoke.py --build MirraChatProbe --port 8767`. For separate browser storage, serve the same build on port 8768 in a second terminal and open one client on each port. Two tabs on the same origin normally restore the same guest, and do not prove independent player identities.

Client A: Create test channel. Copy its channel ID (shown in the text field and `[Mirra browser probe] channel=` diagnostic log) to client B, change B's peer label to B, Join. Send one benign probe from each client; require both new messages to arrive as live events on the other client, not only in history. Read history, leave/rejoin, repeat a send. Channels are isolated test data, not live lobby messages. Leave and disconnect each client after testing. No automatic flood test is included.

Local success does not verify Yandex's CSP. The game pilot stays disabled until server anti-spam and the relevant platform checks are established. Do not copy complete SDK console logs: SDK diagnostics can include authentication URLs. Use only the probe's own safe status lines.
