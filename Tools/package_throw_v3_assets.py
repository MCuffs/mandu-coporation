"""Package selected native generations; crop/pad/downsample only, never upscale.

Input: Build/ThrowAssetsV3/generation-records.json (exported tool prompts/paths).
Requires Pillow. Does not touch runtime Resources or game code.
"""
import json
import hashlib
import shutil
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/DadSchoolBoss/Art'
QA = ROOT / 'Design/dad-throw-v3-review'
SOURCE = QA / 'sources'
BUILD = ROOT / 'Build/ThrowAssetsV3'
ICONS = ['shop_pierce', 'shop_reload', 'shop_twin', 'shop_heavy', 'shop_bounce',
         'shop_blast', 'shop_wide', 'shop_sticky', 'shop_flow']

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def alpha_bounds(im):
    return list(im.getchannel('A').getbbox())

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--catalog',type=Path,help='Optional repo-relative selected-generation catalog')
    args=parser.parse_args()
    QA.mkdir(parents=True, exist_ok=True)
    SOURCE.mkdir(exist_ok=True)
    catalog = args.catalog or BUILD/'generation-records.json'
    if args.catalog and not catalog.is_absolute():
        catalog=ROOT/catalog
    if not catalog.exists():
        catalog = QA/'generation-records.json'
    records = json.loads(catalog.read_text(encoding='utf-8'))
    records = {r['id']:r for r in records}
    manifests = json.loads((BUILD/'vector-records.json').read_text(encoding='utf-8'))
    prompts = {f:[] for f in manifests}
    for name in ICONS:
        record = records[name]
        src = Path(record['source'].split(' as ')[-1])
        if not src.is_absolute():
            src=ROOT/src
        original = Image.open(src).convert('RGBA')
        target = SOURCE/(name+'.png')
        if src.resolve()!=target.resolve():
            shutil.copyfile(src,target)
        bounds = original.getchannel('A').getbbox()
        cropped = original.crop(bounds)
        scale = min(409/cropped.width, 409/cropped.height)
        assert scale <= 1, f'{name}: forbidden upscale'
        size = (round(cropped.width*scale),round(cropped.height*scale))
        reduced = cropped.resize(size,Image.Resampling.LANCZOS)
        out = Image.new('RGBA',(512,512))
        xy = ((512-size[0])//2,(512-size[1])//2)
        out.alpha_composite(reduced,xy)
        dest = ART/'ThrowShopV1'/(name+'.png')
        out.save(dest)
        manifests['ThrowShopV1'].append({
            'id':name,'file':name+'.png','width':512,'height':512,
            'bounds':alpha_bounds(out),'pivotPixels':{'x':256,'y':256},
            'sourceType':'image_gen','source':target.relative_to(ROOT).as_posix(),
            'nativeSourceSize':list(original.size),'sourceCropBounds':list(bounds),
            'resizeScale':scale,'resample':'Lanczos downsample only',
            'sourceSha256':digest(target),'status':'review-candidate'})
        prompts['ThrowShopV1'].append({'id':name,'tool':'image_gen.imagegen','prompt':record['prompt'],
            'references':['Assets/DadSchoolBoss/Resources/DadThrowV1/projectile_golf.png',
                          'Assets/DadSchoolBoss/Resources/DadThrowV1/projectile_shoe.png']})

    # Landmarks were inspected on the native source at 100%. Actual alpha above 8
    # identifies the scalp/sole; low-alpha specks are preserved, not painted out.
    entrances = [
        ('tray_entrance','tray_entrance_attempt5',5,1772,444,'tray/tray_block_0.png'),
        ('chair_entrance','chair_entrance_attempt4',325,1738,443,'chair/chair_windup_0.png'),
        ('dodge_entrance','dodge_entrance_attempt4',211,1723,470,'dodge/dodge_walk_0.png')]
    for name,selected,scalp,sole,foot_x,reference in entrances:
        record=records[selected]
        src=Path(record['source'].split(' as ')[-1])
        if not src.is_absolute():
            src=ROOT/src
        original=Image.open(src).convert('RGBA')
        native_stand=sole-scalp
        scale=1400/native_stand
        assert scale<=1, f'{name}: forbidden upscale'
        target=SOURCE/(name+'.png')
        if src.resolve()!=target.resolve():
            shutil.copyfile(src,target)
        reduced=original.resize((round(original.width*scale),round(original.height*scale)),Image.Resampling.LANCZOS)
        sx,sy=reduced.width/original.width,reduced.height/original.height
        xy=(round(1024-foot_x*sx),round(1900-sole*sy))
        out=Image.new('RGBA',(2048,2048))
        out.alpha_composite(reduced,xy)
        dest=ART/'ThrowEntranceV1'/(name+'.png');out.save(dest)
        manifests['ThrowEntranceV1'].append({'id':name,'file':name+'.png','width':2048,'height':2048,
            'bounds':alpha_bounds(out),'pivotPixels':{'x':1024,'y':1900},'standPixels':1400,'facesLeft':True,
            'sourceType':'image_gen','source':target.relative_to(ROOT).as_posix(),
            'nativeSourceSize':list(original.size),'nativeStandPixels':native_stand,
            'sourceLandmarks':{'scalpY':scalp,'soleY':sole,'feetCenterX':foot_x},
            'resizeScale':scale,'actualResizeScale':{'x':sx,'y':sy},'placementPixels':list(xy),
            'resample':'Lanczos downsample, then transparent padding; no enlargement',
            'sourceSha256':digest(target),'status':'art-review-required',
            'reviewNotes':['Longer proportions than combat reference; approve or redraw before runtime integration.',
                           'Generated fills retain slight tonal variation; strict flat-color art review remains.']})
        prompts['ThrowEntranceV1'].append({'id':name,'tool':'image_gen.imagegen','prompt':record['prompt'],
            'references':['Assets/DadSchoolBoss/Resources/DadV5/chars/'+reference]})

    portable=[]
    for name in ICONS:
        portable.append({'id':name,'prompt':records[name]['prompt'],
                         'source':(SOURCE/(name+'.png')).relative_to(ROOT).as_posix()})
    for name,selected,*_ in entrances:
        portable.append({'id':selected,'prompt':records[selected]['prompt'],
                         'source':(SOURCE/(name+'.png')).relative_to(ROOT).as_posix()})
    (QA/'generation-records.json').write_text(json.dumps(portable,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

    for folder,assets in manifests.items():
        for a in assets:
            p = ART/folder/a['file']
            im = Image.open(p)
            a['bounds'] = alpha_bounds(im)
            a['sha256'] = digest(p)
            if a['sourceType']=='vector':
                a['status']='review-candidate'
                prompts[folder].append({'id':a['id'],'tool':'native SVG / sharp',
                    'brief':'v3 request: '+a['id']+'; exact native canvas, flat fills, black outline, no text.',
                    'source':a['source'],'recipe':'Tools/build_throw_v3_vectors.cjs'})
        (ART/folder/'manifest.json').write_text(json.dumps({'version':1,'coordinateOrigin':'top-left',
            'boundsConvention':'[left,top,right,bottom], right/bottom exclusive; alpha > 0',
            'assets':assets},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        (ART/folder/'prompts.json').write_text(json.dumps({'version':1,'assets':prompts[folder]},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

    # Preview at actual 48px icon size, plus a larger inspection view.
    try:
        font = ImageFont.truetype('arial.ttf',17)
    except OSError:
        font = ImageFont.load_default(size=17)
    sheet = Image.new('RGB',(960,1050),'#e9e4d9')
    draw = ImageDraw.Draw(sheet)
    for i,name in enumerate(ICONS):
        x=(i%3)*320; y=(i//3)*350
        icon=Image.open(ART/'ThrowShopV1'/(name+'.png'))
        large=icon.resize((240,240),Image.Resampling.LANCZOS)
        sheet.paste(large,(x+40,y+12),large)
        small=icon.resize((48,48),Image.Resampling.LANCZOS)
        sheet.paste(small,(x+136,y+260),small)
        draw.text((x+76,y+318),name,font=font,fill='#29272b')
    sheet.save(QA/'shop-icons-review.jpg',quality=94)
    print('Packaged 24 PNG candidates: 12 shop + 6 entrance + 6 FX. Native originals and prompts preserved.')

if __name__=='__main__':
    main()
