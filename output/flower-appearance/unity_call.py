"""Local Unity Pipeline adapter; reads credentials only from this project's descriptor."""
import json
import pathlib
import sys
import urllib.request
import urllib.error

root = pathlib.Path(__file__).resolve().parents[2]
descriptor = json.loads((root / 'Library/Pipeline/.unity-pipeline-port').read_text())
request = urllib.request.Request(
    f"http://127.0.0.1:{descriptor['port']}/api/exec",
    data=json.dumps({'argv': sys.argv[1:], 'timeout': 60000}).encode(),
    headers={'Authorization': 'Bearer ' + descriptor['evalToken'], 'Content-Type': 'application/json'},
)
try:
    with urllib.request.urlopen(request, timeout=65) as response:
        print(response.read().decode())
except urllib.error.HTTPError as error:
    print(error.read().decode())
    sys.exit(1)
