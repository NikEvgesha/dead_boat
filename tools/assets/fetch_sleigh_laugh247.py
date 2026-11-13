"""Fetch the public Freesound preview and preserve its source/license evidence.

The original WAV requires login; this deliberately uses the publicly offered
preview, which is the same CC0 work, without authentication or private endpoints.
"""
import hashlib
import json
from pathlib import Path
import re
import urllib.request

PAGE = "https://freesound.org/people/remarknl/sounds/197411/"
FOLDER = Path("ArtSource/NewYearBoss/Audio247")
FOLDER.mkdir(parents=True,exist_ok=True)
request=urllib.request.Request(PAGE,headers={"User-Agent":"Mozilla/5.0"})
with urllib.request.urlopen(request,timeout=30) as response:
    html=response.read().decode("utf-8")
(FOLDER/"source-page.html").write_text(html,encoding="utf-8")
urls=sorted(set(re.findall(r'https?[^\s"<>]+\.(?:mp3|ogg)',html)))
urls=[url.replace("&amp;","&") for url in urls if "197411" in url and "previews" in url]
print(json.dumps({"public_preview_candidates":urls},ensure_ascii=False))
if not urls:raise RuntimeError("Public preview URL not found in the source page")
url=next((u for u in urls if "hq.mp3" in u),next((u for u in urls if u.endswith(".mp3")),urls[0]))
with urllib.request.urlopen(urllib.request.Request(url,headers={"User-Agent":"Mozilla/5.0"}),timeout=30) as response:
    payload=response.read()
suffix=".ogg" if url.endswith(".ogg") else ".mp3"
target=FOLDER/("197411_remarknl_evil_chuckle_preview"+suffix)
target.write_bytes(payload)
record={"title":"Evil_chuckle.wav","author":"remarknl","page":PAGE,
    "license":"CC0 1.0","license_url":"https://creativecommons.org/publicdomain/zero/1.0/",
    "retrieved":"2026-11-12","download":"public high-quality preview; original login-only WAV not accessed",
    "preview_url":url,"file":str(target),"sha256":hashlib.sha256(payload).hexdigest(),"bytes":len(payload)}
(FOLDER/"source.json").write_text(json.dumps(record,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps(record,ensure_ascii=False))
