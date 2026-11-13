"""Use complete performed chuckles, not speech syllables or synthesized laughter.

Run with Blender's Python (aud decodes the public MP3 preview, numpy processes it):
blender --background --python tools/assets/build_sleigh_laugh247.py
"""
from array import array
import hashlib
import json
from pathlib import Path
import wave
import aud
import numpy as np

FOLDER=Path("E:/Fork/dead_boat/ArtSource/NewYearBoss/Audio247")
OUT=Path("E:/Fork/dead_boat/Assets/Events/NewYear/Boss244/Audio")
source=FOLDER/"197411_remarknl_evil_chuckle_preview.mp3"
sound=aud.Sound(str(source)).cache()
samples=np.asarray(sound.data(),dtype=np.float64)
rate=int(sound.specs[0])
if samples.ndim>1:samples=samples.mean(axis=1)
samples-=samples.mean() # The source author notes a small DC offset.
frame=max(1,round(rate*.01))
energy=np.array([np.sqrt(np.mean(samples[i:i+frame]**2)) for i in range(0,len(samples),frame)])
threshold=max(.006,float(np.percentile(energy,90))*.12)
active=np.flatnonzero(energy>threshold)
if not len(active):raise RuntimeError("Recording appears silent")
groups=[];start=int(active[0]);last=start
for at in active[1:]:
    at=int(at)
    if at-last>26:
        groups.append((start,last+1));start=at
    last=at
groups.append((start,last+1))
regions=[(max(0,a*.01-.065),min(len(samples)/rate,b*.01+.095)) for a,b in groups if (b-a)*.01>.27]
print("CHUCKLE_REGIONS247 "+json.dumps({"sample_rate":rate,"seconds":len(samples)/rate,"threshold":threshold,"regions":regions}))

def write(path,data,sample_rate):
    with wave.open(str(path),"wb") as clip:
        clip.setparams((1,2,sample_rate,0,"NONE","not compressed"))
        clip.writeframes(array("h",np.rint(np.clip(data,-1,1)*32767).astype(np.int16)).tobytes())

write(FOLDER/"Evil_Chuckle_decoded.wav",samples,rate)
(FOLDER/"regions.json").write_text(json.dumps({"rate":rate,"regions":regions},indent=2)+"\n",encoding="utf-8")
eligible=[(a,b) for a,b in regions if .5<=b-a<=2.4]
if len(eligible)<3:raise RuntimeError("Inspect recording regions before selecting three complete short chuckles")
report=[]
for index,(a,b) in enumerate(eligible[:3]):
    clip=samples[round(a*rate):round(b*rate)].copy()
    # Preserve each actor performance and its inhalation/uneven glottal pulses.
    # Only DC removal, mono conversion, gentle fades, resampling and gain are applied.
    clip-=clip.mean()
    n=round(len(clip)*22050/rate)
    clip=np.interp(np.linspace(0,len(clip)-1,n),np.arange(len(clip)),clip)
    fade=min(round(.012*22050),len(clip)//4)
    clip[:fade]*=np.linspace(0,1,fade);clip[-fade:]*=np.linspace(1,0,fade)
    clip*=.76/max(float(np.max(np.abs(clip))),1e-8)
    path=OUT/f"Sleigh_Chuckle_{index+1}.wav"
    write(path,clip,22050)
    report.append({"file":path.name,"source_seconds":[round(a,3),round(b,3)],"seconds":round(len(clip)/22050,3),
        "peak":round(float(np.max(np.abs(clip))),4),"rms":round(float(np.sqrt(np.mean(clip**2))),4),
        "sha256":hashlib.sha256(path.read_bytes()).hexdigest()})
metadata={"source":"Evil_chuckle.wav by remarknl: complete performed evil chuckles, public high-quality MP3 preview",
    "page":"https://freesound.org/people/remarknl/sounds/197411/","license":"CC0 1.0",
    "license_url":"https://creativecommons.org/publicdomain/zero/1.0/",
    "generator":"tools/assets/build_sleigh_laugh247.py","sample_rate":22050,"channels":1,
    "processing":"DC removal, mono, 22050 Hz PCM16, 12 ms edge fades, peak normalization. No TTS, syllable repetition or pitch shifting.",
    "clips":report}
(FOLDER.parent/"audio247.json").write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("BUILT247 "+json.dumps(report))
