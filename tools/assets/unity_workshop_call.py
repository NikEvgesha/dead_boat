"""Call the local Unity MCP using its current configured host."""
import json
import sys
import urllib.request
from pathlib import Path

config = json.loads(Path('UserSettings/AI-Game-Developer-Config.json').read_text())
host = config['host']
headers = {'Content-Type': 'application/json', 'Accept': 'application/json, text/event-stream'}

def request(method, params, ident):
    payload = json.dumps({'jsonrpc': '2.0', 'id': ident, 'method': method, 'params': params}).encode()
    with urllib.request.urlopen(urllib.request.Request(host, payload, headers), timeout=180) as response:
        session = response.headers.get('Mcp-Session-Id')
        if session:
            headers['Mcp-Session-Id'] = session
        raw = response.read().decode()
        if raw.startswith('event:') or raw.startswith('data:'):
            return json.loads(next(line[5:].strip() for line in raw.splitlines() if line.startswith('data:')))
        return json.loads(raw)

request('initialize', {'protocolVersion': '2024-11-05', 'capabilities': {}, 'clientInfo': {'name': 'asset-workshop', 'version': '1'}}, 1)
args = json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig')) if len(sys.argv) > 2 else {}
result = request('tools/call', {'name': sys.argv[1], 'arguments': args}, 2)
print(json.dumps(result, ensure_ascii=False))
if 'error' in result or result.get('result', {}).get('isError'):
    sys.exit(1)
