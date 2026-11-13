"""Historical DB-ON-245 procedural voice. Current voice: build_sleigh_audio246.py.

Run from the project root. Deterministic mono PCM16, 22050 Hz. Voice uses
pitched glottal harmonics, vowel formants, breath and irregular laugh pulses.
"""
from array import array
import json
import math
from pathlib import Path
import random
import wave

RATE = 22050
OUT = Path("Assets/Events/NewYear/Boss244/Audio")
OUT.mkdir(parents=True, exist_ok=True)


def save(name, values):
    peak = max(abs(v) for v in values) or 1
    values = [v * .76 / peak for v in values]
    data = array("h", (round(v * 32767) for v in values))
    with wave.open(str(OUT / name), "wb") as clip:
        clip.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        clip.writeframes(data.tobytes())
    return {"file": name, "seconds": round(len(values) / RATE, 3),
            "peak": .76, "rms": round(math.sqrt(sum(v*v for v in values)/len(values)),4)}


def chuckle(variation):
    rng = random.Random(245 + variation)
    # Short uneven chuckles; the final syllable rises into a sly 'hee'.
    notes = [(.055,.13,230),(.245,.11,218),(.415,.13,270),(.625,.20,305)]
    duration = .99 + variation*.035
    result = [0.0] * int(duration * RATE)
    for index, (start, length, base) in enumerate(notes):
        start += variation * .009 * index
        phase = 0
        base *= 1 + variation * .045
        for n in range(int(length * RATE)):
            t = n / RATE
            p = t / length
            env = min(1,t/.012) * (1-p)**.6 * min(1,(length-t)/.018)
            pitch = base * (1 + .16 * math.sin(math.pi*p) - .13*p + .025*math.sin(2*math.pi*14*t))
            phase += 2*math.pi*pitch/RATE
            voice = 0
            # 'eh' changes to 'ee', giving a cackle instead of four identical beeps.
            f1 = 650 if index < 3 else 340
            f2 = 1770 if index < 3 else 2300
            for h in range(1, 38):
                hz = pitch * h
                gain = (.55*math.exp(-.5*((hz-f1)/170)**2) +
                        .32*math.exp(-.5*((hz-f2)/240)**2) +
                        .13*math.exp(-.5*((hz-3000)/380)**2) + .035) / h**.45
                voice += math.sin(h*phase) * gain
            breath = rng.uniform(-1,1)*.055*(1-p)**2
            sample = (math.tanh(voice*1.8)*.55 + breath)*env
            at = round(start * RATE) + n
            if at < len(result): result[at] += sample
    # Tiny short-room reflection, not a repeating long voice trail.
    delay = round(.027 * RATE)
    dry = result[:]
    for i in range(delay,len(result)): result[i] += dry[i-delay]*.13
    return result


def throw():
    rng = random.Random(2450)
    duration = .43
    out = []
    low = 0
    phase = 0
    for i in range(round(duration * RATE)):
        t = i/RATE
        p = t/duration
        noise = rng.uniform(-1,1)
        low += .09*(noise-low)
        env = math.sin(math.pi*p)**1.7
        phase += 2*math.pi*(680*(1-p)**1.5+110)/RATE
        air = ((noise-low)*.38 + low*.6 + math.sin(phase)*.13)*env
        # A very short wooden/ribbon tick, followed by a small festive bell tail.
        tick = noise*.10*math.exp(-t*65)
        bell = 0 if t < .12 else (.055*math.sin(2*math.pi*1550*t)+.025*math.sin(2*math.pi*2317*t))*math.exp(-(t-.12)*15)
        out.append(air+tick+bell)
    return out


report = [save(f"Sleigh_Chuckle_{i+1}.wav",chuckle(i)) for i in range(3)]
report.append(save("Gift_Throw.wav",throw()))
Path("ArtSource/NewYearBoss/audio245.json").write_text(json.dumps({
    "source":"Original deterministic procedural synthesis; no external recordings or voice imitation",
    "generator":"tools/assets/build_sleigh_audio245.py", "sample_rate":RATE,"channels":1,"clips":report
},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps(report))
