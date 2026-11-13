"""Check delivered PCM files, safe fades and the boss's preserved Unity GUID links."""
from array import array
import hashlib
import math
from pathlib import Path
import re
import wave

folder=Path("Assets/Events/NewYear/Boss244/Audio")
prefab=Path("Assets/Events/NewYear/Boss244/SleighThief_Boss.prefab").read_text(encoding="utf-8")
slots=prefab.split("  laughClips:",1)[1].split("  throwClip:",1)[0]
lines=["DB-ON-247 real recorded chuckles; file/reference verification 2026-11-12"]
seen=set()
for i in range(1,4):
    path=folder/f"Sleigh_Chuckle_{i}.wav"
    guid=re.search(r"^guid: (\w+)",Path(str(path)+".meta").read_text(),re.M).group(1)
    assert guid in slots,"Boss slot lost its reference: "+path.name
    with wave.open(str(path),"rb") as clip:
        assert (clip.getnchannels(),clip.getsampwidth(),clip.getframerate())==(1,2,22050)
        samples=[v/32768 for v in array("h",clip.readframes(clip.getnframes()))]
    seconds=len(samples)/22050
    peak=max(abs(v) for v in samples)
    rms=math.sqrt(sum(v*v for v in samples)/len(samples))
    assert .65<seconds<1.5 and .65<peak<.9 and rms>.04
    assert abs(samples[0])<1e-4 and abs(samples[-1])<1e-4,"Hard edge could click"
    digest=hashlib.sha256(path.read_bytes()).hexdigest();assert digest not in seen;seen.add(digest)
    lines.append(f"PASS {path.name}: linked GUID {guid}; {seconds:.3f} s; peak={peak:.3f}, RMS={rms:.3f}; silent endpoints; unique recording segment; SHA256={digest}")
lines.append("Scope: file format and preserved Unity GUID references only. Actual Editor playback is recorded separately in sleigh-laugh247-runtime.txt when performed.")
lines.append("Owner next: listen to the delivered sample and in-game playback. Status in_review until artistic acceptance.")
Path("Docs/locations/previews/sleigh-laugh247.txt").write_text("\n".join(lines)+"\n",encoding="utf-8")
print("\n".join(lines))
