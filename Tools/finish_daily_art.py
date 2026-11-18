from pathlib import Path
import math,random,wave,array,json,uuid,re
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1]
art=root/'Assets/DadSchoolBoss/Art'
sound=art/'ThrowAudioV1'
sound.mkdir(exist_ok=True)
rows=[]
for name,duration in [('sfx_envelope',.4),('sfx_stamp',.25),('sfx_chest_open',.6),('sfx_card_deal',.15),('sfx_try_spent',.2),('jingle_daily_clear',1.5)]:
    rng=random.Random(name)
    samples=[]
    last=0
    for i in range(round(48000*duration)):
        t=i/48000
        n=rng.uniform(-1,1)
        hiss=n-last
        last=n
        def tone(f):return math.sin(math.tau*f*t)
        if name=='sfx_envelope':v=hiss*(.2+abs(tone(19))*.7)*math.sin(math.pi*t/duration)**.8
        elif name=='sfx_stamp':v=(tone(85)+.4*tone(178))*math.exp(-23*t)+n*.6*math.exp(-75*t)
        elif name=='sfx_try_spent':v=(tone(182)+n*.8)*math.exp(-48*t)
        elif name=='sfx_card_deal':v=hiss*.22*math.exp(-23*t)+(tone(530)+n)*math.exp(-100*abs(t-.075))
        elif name=='sfx_chest_open':v=(tone(132)+n*.3)*math.exp(-22*t)+sum((math.sin(math.tau*f*(t-s))+.24*math.sin(math.tau*2.6*f*(t-s)))*math.exp(-17*(t-s)) for f,s in [(1100,.15),(1460,.22),(1750,.3)] if t>s)
        else:v=sum((math.sin(math.tau*f*(t-s))+.25*math.sin(math.tau*2*f*(t-s)))*math.exp(-5*(t-s))*min(1,(t-s)*180) for f,s in [(523,0),(659,.18),(784,.35),(1046,.6),(1318,.83)] if t>=s)
        samples.append(v*min(1,(i+1)/32,(duration-t)*90))
    gain=32767*10**(-3/20)/max(map(abs,samples))
    data=array.array('h',(round(v*gain) for v in samples))
    with wave.open(str(sound/(name+'.wav')),'wb') as w:
        w.setparams((1,2,48000,0,'NONE','not compressed'))
        w.writeframes(data.tobytes())
    rows.append(dict(id=name,file=name+'.wav',seconds=duration,sampleRate=48000,channels=1,peakDb=-3))
for filename,items in [('manifest',rows),('prompts',[dict(id=r['id'],method='original procedural synthesis',source='Tools/finish_daily_art.py') for r in rows])]:
    (sound/(filename+'.json')).write_text(json.dumps({'version':1,'assets':items},indent=2)+'\n')
folders=[art/k for k in ['ThrowHudV2','ThrowUiV2','ThrowHomeV1']]
preview=Image.new('RGB',(720,300),'#eee4cb')
draw=ImageDraw.Draw(preview)
rules=[]
for folder in folders:
    manifest=folder/'manifest.json'
    catalog=json.loads(manifest.read_text())
    for row in catalog['assets']:
        im=Image.open(folder/row['file'])
        assert im.mode=='RGBA' and im.size==(row['width'],row['height'])
        alpha=im.getchannel('A')
        row['bounds']=list(alpha.getbbox())
        if row['id']=='fog_veil':assert alpha.getextrema()[1]<=204
        if folder.name=='ThrowHudV2' and row['id'].startswith('rule_') and row['id']!='rule_chip':rules.append((im,row['id']))
    manifest.write_text(json.dumps(catalog,indent=2)+'\n')
for i,(im,name) in enumerate(rules):
    x=i%6*120;y=i//6*150
    for size,offset in [(92,0),(30,94)]:
        icon=im.resize((size,size),Image.Resampling.LANCZOS)
        preview.paste(icon,(x+(120-size)//2,y+offset),icon)
    draw.text((x+12,y+129),name,fill='#212629')
preview.save(root/'Design/daily-rule-icons-preview.png')
for folder in folders+[sound]:
    for file in [folder,*folder.iterdir()]:
        if file.suffix=='.meta':continue
        template={'.png':art/'ThrowShopV1/icon_won.png.meta','.wav':art/'V13Slingshot/audio/sfx_wall_boing.wav.meta','.json':art/'ThrowShopV1/manifest.json.meta'}.get(file.suffix,art/'ThrowShopV1.meta')
        meta=Path(str(file)+'.meta')
        if not meta.exists():
            content=re.sub(r'^guid: .*$', 'guid: '+uuid.uuid5(uuid.NAMESPACE_URL,file.relative_to(root).as_posix()).hex,template.read_text(),count=1,flags=re.M)
            meta.write_text('\n'.join(s.rstrip() for s in content.splitlines())+'\n')
print('Verified 42 PNGs and wrote 6 original WAVs.')
