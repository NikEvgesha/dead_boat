"""Historical rejected DB-ON-246 speech laugh. Current: build_sleigh_laugh247.py.

First run build_sleigh_voice246.ps1 if rebuilding the included source syllable.
The existing present-throw sound is preserved. No outside recordings or voice imitation.
"""
from array import array
import json
import math
from pathlib import Path
import wave

RATE = 22050
SOURCE = Path("ArtSource/NewYearBoss/Audio246/Hi_Source_Irina.wav")
OUT = Path("Assets/Events/NewYear/Boss244/Audio")


def read(path):
    with wave.open(str(path), "rb") as clip:
        if (clip.getnchannels(),clip.getsampwidth(),clip.getframerate()) != (1,2,RATE):
            raise ValueError("Expected mono PCM16 22050 Hz: " + str(path))
        return [n/32768 for n in array("h",clip.readframes(clip.getnframes()))]


def save(path, data):
    peak = max(abs(n) for n in data)
    data = [n*.74/peak for n in data]
    with wave.open(str(path),"wb") as clip:
        clip.setparams((1,2,RATE,0,"NONE","not compressed"))
        clip.writeframes(array("h",(round(n*32767) for n in data)).tobytes())
    return {"file":path.name,"seconds":round(len(data)/RATE,3),"peak":.74,
            "rms":round(math.sqrt(sum(n*n for n in data)/len(data)),4)}


def resample(data, rate):
    result = []
    for i in range(int((len(data)-1)/rate)):
        at = i*rate;lo=int(at);f=at-lo
        result.append(data[lo]*(1-f)+data[lo+1]*f)
    return result


source=read(SOURCE)
threshold=max(abs(n) for n in source)*.018
voiced=[i for i,n in enumerate(source) if abs(n)>threshold]
if not voiced: raise ValueError("Voice source is silent")
source=source[max(0,voiced[0]-round(RATE*.016)):min(len(source),voiced[-1]+round(RATE*.025))]
report=[]
for variation in range(3):
    result=[0.0]*round(RATE*.025)
    # Two distinct aspirated 'хи' syllables, falling rather than a melodic rising beep.
    for index,pitch in enumerate((.96-.025*variation,.87-.02*variation)):
        syllable=resample(source,pitch)
        for i,value in enumerate(syllable):
            edge=min(1,i/(RATE*.008),(len(syllable)-1-i)/(RATE*.015))
            # Mild saturation adds a sneer while keeping the consonant and vowel intact.
            syllable[i]=math.tanh(value*1.35)*edge*(1 if index==0 else .86)
        result.extend(syllable)
        result.extend([0.0]*round(RATE*(.105 if index==0 else .06)))
    report.append(save(OUT/f"Sleigh_Chuckle_{variation+1}.wav",result))

throw=read(OUT/"Gift_Throw.wav")
report.append({"file":"Gift_Throw.wav","seconds":round(len(throw)/RATE,3),"preserved":True})
metadata={"source":"Generic local Microsoft Irina Desktop SAPI voice saying one Russian хи syllable; assembled and lowered into hi-hi. No human recording or imitation.",
          "source_file":str(SOURCE),"source_generator":"tools/assets/build_sleigh_voice246.ps1",
          "generator":"tools/assets/build_sleigh_audio246.py","sample_rate":RATE,"channels":1,"clips":report}
Path("ArtSource/NewYearBoss/audio246.json").write_text(json.dumps(metadata,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps(report,ensure_ascii=False))
