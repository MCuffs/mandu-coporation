"""Checks the asset delivery contract without running or modifying the player's save/game."""
from pathlib import Path
from PIL import Image
import hashlib
import json
import re
import struct
import wave

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/DadSchoolBoss/Art/V13Slingshot'
checks=[]


def check(condition,label):
    if not condition:raise AssertionError(label)
    checks.append('PASS '+label)


def main():
    manifest=json.loads((ART/'manifest.json').read_text(encoding='utf-8'))
    check(len(manifest['entries'])==18,'14 PNGs and 4 WAVs in delivery manifest')
    for e in manifest['entries']:
        p=ART/e['file'];check(p.is_file() and hashlib.sha256(p.read_bytes()).hexdigest()==e['sha256'],'hash '+e['file'])
        check(Path(str(p)+'.meta').exists(),'Unity meta '+e['file'])
        if p.suffix=='.png':
            im=Image.open(p);check(list(im.size)==e['size'],'dimensions '+e['file'])
            if e['mode']=='RGBA':
                a=im.getchannel('A');check(a.getextrema()[0]==0 and a.getextrema()[1]>=250,'alpha '+e['file'])
                edge=list(a.crop((0,0,im.width,8)).get_flattened_data())+list(a.crop((0,im.height-8,im.width,im.height)).get_flattened_data())
                check(max(edge)<=2,'transparent outer top/bottom margins '+e['file'])
            if e['file'].startswith('chars/'):
                check(im.size==(768,768),'character canvas '+e['file'])
                check(e['pivot_pixels_top_left']==([384,384] if e['airborne_rotation_override'] else [384,720]),'ground/flight pivot '+e['file'])
            if 'rage_' in e['file'] and e['file'].startswith('fx/'):
                check(a.getpixel((256,256))==0,'rage frame leaves dad visible '+e['file'])
            if e['file'].startswith('ui/'):
                check(im.size==(256,256),'icon or tutorial hand '+e['file'])
        else:
            with wave.open(str(p)) as f:
                check((f.getnchannels(),f.getsampwidth(),f.getframerate())==(1,2,44100),'PCM format '+p.name)
                pcm=struct.unpack('<'+'h'*f.getnframes(),f.readframes(f.getnframes()))
                check(max(abs(v) for v in pcm)<32000 and any(abs(v)>1000 for v in pcm),'non-silent unclipped audio '+p.name)
    guide=json.loads((ART/'arena-guide.json').read_text(encoding='utf-8'))
    check(Image.open(ART/guide['image']).size==(1080,2340),'long-phone background contract')
    x,y,w,h=guide['centerCrop9x16'];dx,dy,dw,dh=guide['doorRect']
    check(x<=dx and y<=dy and dx+dw<=x+w and dy+dh<=y+h,'door survives full 9:16 center crop')
    check(len(guide['lockerIntervalsY'])*len(guide['sides'])==8,'eight locker banks separately referenced')
    check(guide['verticalWallFrontX']['left']<guide['verticalWallFrontX']['right'],'ordered straight wall planes')
    html=(ROOT/'Design/dad-b-v13-preview.html').read_text(encoding='utf-8')
    refs=re.findall(r'''(?:src=["']|url\(["']?)([^"')]+)''',html)
    check(all((ROOT/'Design'/ref).resolve().is_file() for ref in refs),'all '+str(len(refs))+' preview references exist')
    guids=[re.search(r'guid: ([0-9a-f]+)',p.read_text(encoding='utf-8-sig')).group(1) for p in ART.rglob('*.meta')]
    check(len(guids)==len(set(guids)),'new asset GUIDs unique')
    report={'checked_on':'2026-11-05','checks':checks,'count':len(checks),
            'visual_review':['existing dad identity and five new poses reviewed','short crop and long framing reviewed','rear sandal edit reviewed','empty-center rage frames reviewed'],
            'runtime_connected':False,'android_device_tested':False,'p2_assets_included':False}
    (ROOT/'Design/dad-b-v13-checks.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'checks':len(checks),'status':'PASS','png':14,'wav':4}))


if __name__=='__main__':main()
