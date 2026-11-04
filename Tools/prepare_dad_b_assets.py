"""Mechanical delivery preparation, matching the project's v7 asset workflow.
Original generated PNGs remain untouched in _sources. No pose repainting or shape warping.
Ground sprites use the v5 768 canvas / (384,720) root; flight sprites explicitly carry a center rotation pivot.
"""
from pathlib import Path
from PIL import Image
import hashlib
import json
import re
import uuid

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/DadSchoolBoss/Art/V13Slingshot'
SRC = OUT / '_sources'
entries = []


def source(name):
    corrected = SRC / (name+'.corrected.png')
    return corrected if corrected.exists() else SRC / (name+'.png')


def bounds(im):
    return im.getchannel('A').point(lambda a: 255 if a > 40 else 0).getbbox()


def meta(path, transparent):
    ref = ROOT / ('Assets/DadSchoolBoss/Resources/DadV5/chars/dad/dad_idle_0.png.meta' if transparent
                  else 'Assets/DadSchoolBoss/Resources/DadV5/bg/bg_floor1_corridor.png.meta')
    text = ref.read_text(encoding='utf-8-sig')
    text = re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,text,count=1)
    text = re.sub(r'maxTextureSize: \d+','maxTextureSize: '+('4096' if path.parent.name=='bg' else '1024'),text)
    p=Path(str(path)+'.meta')
    if not p.exists():p.write_text(text,encoding='utf-8')


def save(im, rel, request_id, extra=None):
    p=OUT/rel;p.parent.mkdir(parents=True,exist_ok=True);im.save(p)
    item={'file':rel,'request_id':request_id,'size':[im.width,im.height],'mode':im.mode,
          'runtime_connected':False,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
    if im.mode=='RGBA':item['visible_bounds']=list(bounds(im));item['alpha_range']=list(im.getchannel('A').getextrema())
    if extra:item.update(extra)
    entries.append(item);meta(p,im.mode=='RGBA')


def sprite(name, rel, scale, airborne=False, wall=False):
    im=Image.open(source(name)).convert('RGBA')
    # Same residual-alpha cleanup threshold used by prepare_dad_motion_v7.py.
    im.putalpha(im.getchannel('A').point(lambda a: 0 if a<24 else a))
    box=bounds(im);art=im.crop(box)
    scale=min(scale,704/art.width,640/art.height)
    art=art.resize((round(art.width*scale),round(art.height*scale)),Image.Resampling.LANCZOS)
    x=384-art.width//2;y=384-art.height//2 if airborne else 720-art.height
    canvas=Image.new('RGBA',(768,768));canvas.alpha_composite(art,(x,y))
    pivot=[384,384] if airborne else [384,720]
    extra={'source':source(name).name,'uniform_scale':scale,'source_bounds':list(box),'placement':[x,y],
           'pivot_pixels_top_left':pivot,'pivot_unity_normalized':[pivot[0]/768,1-pivot[1]/768],
           'standard_ground_pivot':[384,720],'airborne_rotation_override':airborne,'facing':'right'}
    if wall:extra['back_contact_pixels_top_left']=[x,y+round(art.height*.42)]
    save(canvas,rel,'A-B'+('02' if 'pull' in name else '03' if 'flight' in name else '04' if wall else '05'),extra)


def isolate(im, box, size):
    crop=im.crop(box).convert('RGBA');b=bounds(crop);art=crop.crop(b)
    art.thumbnail((size-32,size-32),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(size,size));canvas.alpha_composite(art,((size-art.width)//2,(size-art.height)//2))
    return canvas


def main():
    bg=Image.open(source('AB01-corridor')).convert('RGB')
    save(bg.resize((1080,2340),Image.Resampling.LANCZOS),'bg/corridor_floor1_long.png','A-B01',
         {'source':source('AB01-corridor').name,'center_crop_9_16_pixels':[0,210,1080,1920],
          'wall_coordinates':'See arena-guide.json; sampled after visual review.'})
    sprite('AB02-dad-pull','chars/dad_pull_0.png',.63)
    sprite('AB03-dad-flight-a','chars/dad_flight_0.png',.63,True)
    sprite('AB03-dad-flight-b','chars/dad_flight_1.png',.63,True)
    sprite('AB04-dad-wall-bounce','chars/dad_bounce_0.png',.60,False,True)
    sprite('AB05-dad-land','chars/dad_land_0.png',.63)
    fx=Image.open(source('AB06-07-flight-fx')).convert('RGBA')
    for i,name in enumerate(['wind','swoosh','rage_0','rage_1']):
        box=(i%2*fx.width//2,i//2*fx.height//2,(i%2+1)*fx.width//2,(i//2+1)*fx.height//2)
        save(isolate(fx,box,512),'fx/'+name+'.png','A-B06' if i<2 else 'A-B07',{'source_cell_top_left':list(box),'pivot_unity_normalized':[.5,.5]})
    hand=Image.open(source('AB08-drag-hand')).convert('RGBA')
    save(isolate(hand,(0,0,hand.width,hand.height),256),'ui/drag_hand.png','A-B08')
    icons=Image.open(source('AB09-special-icons')).convert('RGBA')
    for i,name in enumerate(['wall_launch','chain_launch','rage_launch']):
        # First-cell gutter contains a tiny fragment of the neighboring symbol in the generated atlas.
        # Its intended locker ends well inside the cell; keep the gutter outside the export.
        box=(i*icons.width//3,0,(i+1)*icons.width//3-(12 if i==0 else 0),icons.height)
        save(isolate(icons,box,256),'ui/'+name+'.png','A-B09',{'source_cell_top_left':list(box)})
    for p in sorted((OUT/'audio').glob('*.wav')):
        entries.append({'file':p.relative_to(OUT).as_posix(),'request_id':'A-B13','sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'runtime_connected':False})
    manifest={'version':13,'scope':'P0 and P1 B slingshot assets; P2 is deferred to G1 decision',
              'reference_character':'Resources/DadV5/chars/dad/dad_idle_0.png','runtime_connected':False,
              'generated_art':'built-in image_gen','mechanical_preparation':Path(__file__).name,'entries':entries}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'prepared_files':len(entries),'png_files':sum(1 for e in entries if e['file'].endswith('.png'))}))


if __name__=='__main__':main()
