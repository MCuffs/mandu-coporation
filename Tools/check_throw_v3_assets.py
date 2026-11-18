"""File QA and review proofs. Does not launch Unity or modify runtime files."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageChops

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/DadSchoolBoss/Art'
QA=ROOT/'Design/dad-throw-v3-review'
EXPECTED={
 'ThrowEntranceV1':{'tray_entrance':(2048,2048),'chair_entrance':(2048,2048),'dodge_entrance':(2048,2048),
                    'entrance_band_lines':(2048,512),'entrance_burst':(1536,1536),'entrance_balloon':(1024,320)},
 'ThrowShopV1':{**{f'shop_{n}':(512,512) for n in ['pierce','reload','twin','heavy','bounce','blast','wide','sticky','flow']},
                **{n:(256,256) for n in ['icon_won','icon_shop','mark_soldout']}},
 'ThrowFxV2':{**{f'fx_tray_clang_{i}':(256,256) for i in range(3)},**{f'fx_coin_pop_{i}':(128,128) for i in range(3)}}}

def font(size):
    try:return ImageFont.truetype('arial.ttf',size)
    except OSError:return ImageFont.load_default(size=size)

def nine_slice(im,size,c=96):
    result=Image.new('RGBA',size)
    xs=[0,c,im.width-c,im.width];ys=[0,c,im.height-c,im.height]
    tx=[0,c,size[0]-c,size[0]];ty=[0,c,size[1]-c,size[1]]
    for row in range(3):
        for col in range(3):
            piece=im.crop((xs[col],ys[row],xs[col+1],ys[row+1]))
            piece=piece.resize((tx[col+1]-tx[col],ty[row+1]-ty[row]),Image.Resampling.NEAREST)
            result.alpha_composite(piece,(tx[col],ty[row]))
    return result

def main():
    checks=[]
    def check(ok,msg):
        checks.append({'pass':bool(ok),'check':msg})
    for folder,wanted in EXPECTED.items():
        manifest=json.loads((ART/folder/'manifest.json').read_text(encoding='utf-8'))
        assets={a['id']:a for a in manifest['assets']}
        check(set(assets)==set(wanted),folder+' exact requested IDs/count')
        for name,size in wanted.items():
            a=assets[name];p=ART/folder/a['file'];im=Image.open(p)
            check(im.mode=='RGBA' and im.size==size,name+' RGBA/canvas')
            alpha=im.getchannel('A');bounds=alpha.getbbox()
            check(list(bounds)==a['bounds'],name+' measured alpha bounds')
            check(alpha.getextrema()[0]==0 and alpha.getextrema()[1]>0,name+' actual transparency/content')
            check(hashlib.sha256(p.read_bytes()).hexdigest()==a['sha256'],name+' checksum')
            if name.startswith('shop_'):
                l,t,r,b=bounds
                check(min(l,t,512-r,512-b)>=51,name+' at least10percent clear margin')
            if 'resizeScale' in a:
                check(0<a['resizeScale']<=1,name+' no raster upscaling')
                source=ROOT/a['source'];src=Image.open(source)
                check(list(src.size)==a['nativeSourceSize'],name+' native source retained')
            if name.endswith('_entrance'):
                check(a['pivotPixels']=={'x':1024,'y':1900} and a['standPixels']==1400,name+' foot/standing metadata')
                check(a['nativeStandPixels']>=1400 and bounds[1]>=60,name+' native body resolution/top clearance')
                pos=a['placementPixels'];land=a['sourceLandmarks'];s=a['actualResizeScale']
                check(abs(pos[0]+land['feetCenterX']*s['x']-1024)<=1 and abs(pos[1]+land['soleY']*s['y']-1900)<=1,name+' transformed foot within1px')
                check(abs(a['nativeStandPixels']*s['y']-1400)<=1,name+' transformed body height within1px')
            if not a.get('tileX'):
                edges=[alpha.crop((0,0,1,im.height)),alpha.crop((im.width-1,0,im.width,im.height)),alpha.crop((0,0,im.width,1)),alpha.crop((0,im.height-1,im.width,im.height))]
                check(all(e.getextrema()[1]==0 for e in edges),name+' transparent canvas border')
    band=Image.open(ART/'ThrowEntranceV1/entrance_band_lines.png')
    check(band.crop((0,0,1,512)).tobytes()==band.crop((2047,0,2048,512)).tobytes(),'band left/right pixel seam identical')
    burst=Image.open(ART/'ThrowEntranceV1/entrance_burst.png')
    check(burst.getchannel('A').crop((550,550,986,986)).getextrema()[1]==0,'burst center transparent')
    for prefix,pivot in [('fx_tray_clang',{'x':128,'y':128}),('fx_coin_pop',{'x':64,'y':108})]:
        manifest=json.loads((ART/'ThrowFxV2/manifest.json').read_text())
        check(all(a['pivotPixels']==pivot for a in manifest['assets'] if a.get('animation')==prefix),prefix+' fixed frame pivot')

    # Visual proofs, separate from runtime screenshots.
    proof=Image.new('RGB',(1440,820),'#e9e4d9');draw=ImageDraw.Draw(proof)
    for i,name in enumerate(['tray','chair','dodge']):
        im=Image.open(ART/'ThrowEntranceV1'/(name+'_entrance.png'))
        reduced=im.resize((761,761),Image.Resampling.LANCZOS)
        proof.paste(reduced,(i*480-140,-30),reduced)
        draw.line((i*480,676,i*480+480,676),fill='#d29b58',width=2)
        draw.text((i*480+65,735),name+' / 520px body',font=font(22),fill='#29272b')
    proof.save(QA/'entrance-review.jpg',quality=94)
    decor=Image.new('RGB',(1500,1280),'#e9e4d9');d=ImageDraw.Draw(decor)
    for i in range(2):
        b=band.resize((750,188),Image.Resampling.LANCZOS);decor.paste(b,(i*750,40),b)
    d.text((25,12),'Horizontal tile seam at x750',font=font(18),fill='#29272b')
    bs=burst.resize((540,540),Image.Resampling.LANCZOS);decor.paste(bs,(20,255),bs)
    balloon=Image.open(ART/'ThrowEntranceV1/entrance_balloon.png')
    for y,size in [(300,(880,280)),(650,(600,400))]:
        b=nine_slice(balloon,size);decor.paste(b,(590,y),b)
    d.text((600,255),'96px fixed-corner nine-slice proof',font=font(18),fill='#29272b')
    for row,prefix in enumerate(['fx_tray_clang','fx_coin_pop']):
        for i in range(3):
            im=Image.open(ART/'ThrowFxV2'/f'{prefix}_{i}.png')
            x=40+i*172;y=880+row*175
            # FX shown at1:1 (coin) and0.5 (clang), never auto-centered by bounds.
            if im.width==256:im=im.resize((128,128),Image.Resampling.LANCZOS)
            decor.paste(im,(x,y),im);d.text((x,y+132),f'{prefix}_{i}',font=font(13),fill='#29272b')
    for i,name in enumerate(['icon_won','icon_shop','mark_soldout']):
        im=Image.open(ART/'ThrowShopV1'/(name+'.png')).resize((128,128),Image.Resampling.LANCZOS)
        decor.paste(im,(650+i*220,1100),im)
    decor.save(QA/'decor-fx-review.jpg',quality=94)
    report={'assetCount':24,'checks':len(checks),'failures':[c for c in checks if not c['pass']],
            'scope':'File/geometry QA only; not art approval, Unity integration, device QA or gameplay verification.',
            'artReviewPending':['Entrance proportions/pose fidelity and strict flat-color shading.'],
            'results':checks}
    (QA/'qa-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f"{len(checks)} checks; {len(report['failures'])} failures; 24 PNG candidates.")
    for fail in report['failures']:print('FAIL:',fail['check'])
    if report['failures']:raise SystemExit(1)

if __name__=='__main__':main()
