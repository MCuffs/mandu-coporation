"""Sample-free, original procedural audio for the dad game."""
from pathlib import Path
import json, wave
import numpy as np

SR = 44100
rng = np.random.default_rng(73105)

def tone(f, d, decay=10):
    t = np.arange(int(SR*d))/SR
    phase = np.cumsum(np.broadcast_to(f, t.shape))*2*np.pi/SR
    return (np.sin(phase)+.22*np.sin(phase*3))*np.exp(-t*decay)*np.minimum(1,t*1500)

def noise(d, decay=20, smooth=5):
    t=np.arange(int(SR*d))/SR
    n=np.convolve(rng.normal(0,1,len(t)),np.ones(smooth)/smooth,mode='same')
    return n*np.exp(-t*decay)*np.minimum(1,t*1500)

def mix(*sounds):
    out=np.zeros(max(map(len,sounds)))
    for s in sounds: out[:len(s)]+=s
    return out

def melody(notes,d,step=.1,length=.25):
    out=np.zeros(int(SR*d))
    for i,m in enumerate(notes):
        s=tone(440*2**((m-69)/12),length,10)
        p=int(i*step*SR);n=min(len(s),len(out)-p)
        if n>0:out[p:p+n]+=s[:n]
    return out

def music(battle):
    out=np.zeros(SR*60);beat=60/(128 if battle else 96)
    roots=[48,45,53,55];pattern=[0,7,12,4,7,14,12,7]
    def add(s,at,gain):
        p=int(at*SR);n=min(len(s),len(out)-p)
        if n>0:out[p:p+n]+=s[:n]*gain
    for i in range(round(60/beat)):
        at=i*beat;root=roots[(i//8)%4]
        add(tone(440*2**((root-12-69)/12),beat*.8,7),at,.3)
        if i%2==0:
            for semitone in (0,4 if (i//8)%4!=1 else 3,7):add(tone(440*2**((root+semitone-69)/12),beat*1.5,5),at,.06)
        for half in range(2 if battle else 1):
            m=root+12+pattern[(i*(2 if battle else 1)+half)%8]
            add(tone(440*2**((m-69)/12),beat*.6,12),at+half*beat/2,.15)
        if battle:
            add(mix(tone(65,.13,25),noise(.05,60)*.2),at,.3)
            if i%2:add(noise(.1,30),at,.1)
            add(noise(.035,90,1),at+beat/2,.04)
    n=int(.012*SR);out[:n]*=np.linspace(0,1,n);out[-n:]*=np.linspace(1,0,n)
    return out

def main(dest):
    dest=Path(dest);dest.mkdir(parents=True,exist_ok=True);sounds={}
    for i in range(1,4):
        d=.16+i*.025;t=np.arange(int(d*SR))/SR
        sounds[f'sfx_punch_{i}']=mix(tone(140+i*13-t*180,d,24),noise(d,34,9)*.6)
    for i in (1,2):
        d=.5+i*.08;sounds[f'sfx_wall_{i}']=mix(noise(d,20)*.5,*(tone(f*(1+i*.03),d,8)*.2 for f in (106,283,547,891)))
    sounds['sfx_chain']=mix(tone(105,.23,24),noise(.23,30,11)*.75)
    sounds['sfx_tray_block']=mix(*(tone(f,.5,10)*.25 for f in (620,1037,1749)))
    sounds['sfx_tray_break']=mix(noise(.7,14,3)*.5,tone(189,.7,8)*.5)
    sounds['sfx_chair_dash']=noise(.5,3,4)*np.sin(np.linspace(0,np.pi,int(SR*.5)))**2
    t=np.arange(SR)/SR;sounds['sfx_boss_enrage']=mix(tone(130-t*55+np.sin(t*48)*8,1,1.5)*(1+np.sin(t*31)*.3),noise(1,3,13)*.5)
    sounds['sfx_coin']=melody([88,95],.25,.07)
    sounds['sfx_skill_pick']=melody([72,76,79],.5,.07)
    sounds['sfx_button']=mix(tone(420,.1,45),noise(.1,65,7)*.2)
    for name,count in [('common',2),('rare',4),('legend',6)]:sounds['sfx_box_'+name]=mix(noise(.18,28,9)*.4,melody([60,64,67,72,76,79][:count],.95))
    sounds['sfx_revive']=melody([60,64,67,72,79],.95,.09)
    sounds['jingle_win']=melody([60,64,67,72,79,72],3,.38,.65)
    sounds['jingle_lose']=melody([67,63,60,48],2.6,.43,.7)
    sounds['bgm_battle']=music(True);sounds['bgm_home']=music(False);manifest=[]
    for name,data in sounds.items():
        n=min(265,len(data)//4);data[-n:]*=np.linspace(1,0,n);data*=.78/max(float(np.max(np.abs(data))),.001)
        with wave.open(str(dest/(name+'.wav')),'wb') as f:
            f.setnchannels(1);f.setsampwidth(2);f.setframerate(SR);f.writeframes(np.round(data*32767).astype('<i2').tobytes())
        manifest.append({'file':name+'.wav','seconds':round(len(data)/SR,3),'sample_rate':SR,'channels':1,'bits':16})
    (dest/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (dest/'LICENSES.txt').write_text('Original procedural audio for Dad asset pack v5.\nSource: Tools/dad_assets_v5_audio.py. All sound is synthesized from mathematical oscillators, filtered pseudorandom noise and original note patterns. No external recordings, sample libraries, existing songs or human voices are used. The source recipe and outputs are offered under CC0 1.0 Universal to the extent rights apply: https://creativecommons.org/publicdomain/zero/1.0/\nThe boss roar is a synthetic growl. Music is provisional original synthesized music; audition in-game before release.\n')
    print(f'Created {len(manifest)} WAV files in {dest}')

if __name__=='__main__':
    import sys
    main(sys.argv[1])
