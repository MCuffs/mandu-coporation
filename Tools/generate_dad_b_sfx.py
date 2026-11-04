"""Original procedural slingshot cues; no external recordings. Writes 44.1kHz PCM16 mono WAV."""
from pathlib import Path
import json
import wave
import math
import random
import struct

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/DadSchoolBoss/Art/V13Slingshot/audio'
RATE = 44100
rng = random.Random(1313)


def noise(n, low, high):
    lo=hi=0.; signal=[]
    a=1-math.exp(-2*math.pi*high/RATE); b=1-math.exp(-2*math.pi*low/RATE)
    for _ in range(n):
        lo+=a*(rng.gauss(0,1)-lo);hi+=b*(lo-hi);signal.append(lo-hi)
    scale=math.sqrt(sum(x*x for x in signal)/n)
    return [x/max(.001,scale) for x in signal]


def write(name, signal, peak=.56):
    signal = list(signal)
    fade = min(220, len(signal) // 8)
    for i in range(fade):signal[i]*=i/fade;signal[-1-i]*=i/fade
    scale=peak/max(.001,max(abs(x) for x in signal));signal=[x*scale for x in signal]
    pcm = [round(x*32767) for x in signal]
    with wave.open(str(OUT / name), 'wb') as f:
        f.setparams((1, 2, RATE, len(pcm), 'NONE', 'not compressed'))
        f.writeframes(struct.pack('<'+'h'*len(pcm),*pcm))
    return {'file': name, 'seconds': len(pcm)/RATE, 'sample_rate': RATE, 'channels': 1,
            'peak': max(abs(x) for x in signal), 'rms': math.sqrt(sum(x*x for x in signal)/len(signal))}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    entries = []
    sin,exp,pi=math.sin,math.exp,math.pi
    n=int(RATE*.8);nz=noise(n,500,2200)
    signal=[(.42*sin(2*pi*130*t)+.19*sin(2*pi*260*t)+.07*sin(2*pi*390*t))*(.85+.15*sin(2*pi*5*t))+.025*nz[i] for i in range(n) for t in [i/RATE]]
    entries.append(write('sfx_pull_tension.wav',signal,.32))
    n=int(RATE*.36);nz=noise(n,350,6500)
    signal=[(nz[i]*.8+sin(2*pi*(650*t-650*t*t))*.12)*sin(pi*t/.36)**1.3*exp(-t*4) for i in range(n) for t in [i/RATE]]
    entries.append(write('sfx_launch_whoosh.wav',signal))
    n=int(RATE*.32);nz=noise(n,180,3500)
    signal=[.6*sin(2*pi*(150*t-115*t*t))*exp(-t*15)+.20*sin(2*pi*440*t)*exp(-t*25)+.12*sin(2*pi*730*t)*exp(-t*32)+nz[i]*.13*exp(-t*55) for i in range(n) for t in [i/RATE]]
    entries.append(write('sfx_wall_boing.wav',signal,.60))
    n=int(RATE*.28);nz=noise(n,600,5000)
    signal=[.5*sin(2*pi*(90*t-95*t*t))*exp(-t*24)+nz[i]*.22*exp(-t*16)*(1-exp(-t*180)) for i in range(n) for t in [i/RATE]]
    entries.append(write('sfx_landing_skid.wav',signal,.48))
    (OUT/'audio-manifest.json').write_text(json.dumps({'method':'original procedural synthesis; no external samples',
        'usage': {'pull_pitch_range':[.8, 1.8], 'pull_loop':True, 'launch_wall_land':'one shot at event; pool concurrent voices'},
        'files':entries}, indent=2), encoding='utf-8')
    print(json.dumps(entries))


if __name__ == '__main__':
    main()
