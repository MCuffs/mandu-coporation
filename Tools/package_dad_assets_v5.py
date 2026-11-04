"""Bake generated sheets into requested canvases; never redraw source artwork."""
from pathlib import Path
import argparse, json, shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFont

def cutout(image):
    # Export a hard sprite matte: discard the generation tool's semitransparent outer halo.
    # The untouched generated image is retained in _sources for alternate import settings.
    image = image.convert('RGBA')
    image.putalpha(image.getchannel('A').point(lambda a: 255 if a >= 220 else 0))
    return image

def cells(job, image):
    cols, rows = job.get('cols', 1), job.get('rows', 1)
    result = []
    for i, item in enumerate(job['items']):
        x, y = i % cols, i // cols
        rect = (round(x*image.width/cols), round(y*image.height/rows), round((x+1)*image.width/cols), round((y+1)*image.height/rows))
        cell = cutout(image.crop(rect)); bounds = cell.getbbox()
        if bounds is None: raise ValueError('Empty sprite: '+item[0])
        result.append((item[0], cell.crop(bounds), bounds, rect))
    return result

def fit_icon(image, size=256):
    scale = (size-32)/max(image.size)
    image = image.resize((max(1,round(image.width*scale)),max(1,round(image.height*scale))),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(size,size));canvas.alpha_composite(image,((size-image.width)//2,(size-image.height)//2))
    return canvas

def feet_x(image):
    a=np.array(image.getchannel('A'))
    # Both feet, including a lifted trailing foot, belong to the pivot estimate.
    ys,xs=np.where(a[max(0,image.height-round(image.height*.25)):,:]>128)
    return (float(xs.min())+float(xs.max())+1)/2 if len(xs) else image.width/2

def preview(root, files, name, columns=6):
    width=columns*180;rows=(len(files)+columns-1)//columns
    page=Image.new('RGB',(width,rows*205),(225,225,215));draw=ImageDraw.Draw(page)
    for i,path in enumerate(files):
        im=Image.open(root/path).convert('RGBA');im.thumbnail((160,165))
        x=(i%columns)*180;y=(i//columns)*205
        page.paste(im,(x+(180-im.width)//2,y+(170-im.height)//2),im)
        draw.text((x+4,y+174),Path(path).stem,fill=(25,25,25))
    page.save(root/name)

def main():
    ap=argparse.ArgumentParser();ap.add_argument('manifest');ap.add_argument('destination',type=Path);args=ap.parse_args()
    jobs=json.loads(args.manifest if args.manifest.startswith('[') else Path(args.manifest).read_text());root=args.destination;root.mkdir(parents=True,exist_ok=True)
    source_dir=root/'_sources';source_dir.mkdir(exist_ok=True)
    inventory={};warnings=[]
    final_source={item[0]:job['key'] for job in jobs for item in job['items']}
    for job in jobs:
        src=Path(job['source']);shutil.copy2(src,source_dir/(job['key']+'.png'))
        image=Image.open(src).convert('RGBA');group=job['group']
        if group in ('bg','story','app'):
            name=job['items'][0][0];size=tuple(job['size']);dest=root/group/(name+'.png');dest.parent.mkdir(parents=True,exist_ok=True)
            if group=='bg':
                # Snap the back-wall/floor boundary to the exact requested y=480 guide.
                breaks={'bg_floor1_corridor':(.148,.277),'bg_floor2_cafeteria':(.12,.24),'bg_floor3_gym':(.145,.2475)}[name]
                source_y=[0,round(image.height*breaks[0]),round(image.height*breaks[1]),image.height]
                target_y=[0,290,480,1920];aligned=Image.new('RGB',size)
                for k in range(3):
                    band=image.crop((0,source_y[k],image.width,source_y[k+1])).convert('RGB').resize((size[0],target_y[k+1]-target_y[k]),Image.Resampling.LANCZOS)
                    aligned.paste(band,(0,target_y[k]))
                aligned.save(dest)
            else:image.convert('RGB').resize(size,Image.Resampling.LANCZOS).save(dest)
            inventory[str(dest.relative_to(root))]={'size':size,'source':job['key'],'opaque':True};continue
        pieces=cells(job,image)
        character=group.startswith('chars/')
        scale=1
        if character and not job.get('fly'):
            nominal=640 if group.split('/')[-1].startswith('boss') else 560
            if job['key']=='char_dad_combat':nominal=530
            if job['key'].startswith(('walkfix_','fix_boss2_dash')):
                key=group+'/'+group.split('/')[-1]+'_walk_0.png'
                nominal=inventory.get(key,{}).get('content_height',nominal)
                if job['key']=='fix_boss2_dash':nominal*=.84
            # Fixed scale within a sheet; don't independently stretch each pose.
            reference_height=pieces[0][1].height
            scale=nominal/reference_height
            active=[p for p in pieces if final_source[p[0]]==job['key']] or pieces
            limit=min(min(376/max(1,feet_x(p[1])),376/max(1,p[1].width-feet_x(p[1])),712/p[1].height) for p in active)
            if scale>limit:
                warnings.append(job['key']+': wide pose limits common scale; original sheet retained for manual animation review')
                scale=limit
        for name,piece,bounds,rect in pieces:
            if final_source[name]!=job['key']:continue
            if group=='props':
                folder='chars/tray' if name=='prop_tray_dented' else 'chars/dodge';size=256 if name=='prop_tray_dented' else 128
                canvas=fit_icon(piece,size);meta={'size':[size,size]}
            elif not character:
                folder=group;canvas=fit_icon(piece);meta={'size':[256,256]}
            else:
                folder=group;canvas=Image.new('RGBA',(768,768))
                if job.get('fly'):
                    k=650/max(piece.size);scaled=piece.resize((round(piece.width*k),round(piece.height*k)),Image.Resampling.LANCZOS)
                    pos=((768-scaled.width)//2,(768-scaled.height)//2);meta={'size':[768,768],'center':[384,384],'flying':True}
                else:
                    anchor=feet_x(piece);scaled=piece.resize((max(1,round(piece.width*scale)),max(1,round(piece.height*scale))),Image.Resampling.LANCZOS)
                    pos=(round(384-anchor*scale),720-scaled.height)
                    if pos[0]<0 or pos[0]+scaled.width>768 or pos[1]<0:
                        warnings.append(name+': canvas overflow; use original sheet for manual correction')
                    meta={'size':[768,768],'feet':[384,720],'pivot_unity':[.5,.0625],'scale':round(scale,6),'content_height':scaled.height,'animation_review_required':True}
                canvas.alpha_composite(scaled,pos)
            dest=root/folder/(name+'.png');dest.parent.mkdir(parents=True,exist_ok=True);canvas.save(dest)
            meta.update({'source':job['key'],'source_cell':list(rect),'source_bounds_in_cell':list(bounds),'matte_alpha_threshold':220})
            inventory[str(dest.relative_to(root))]=meta
    checks=[]
    for path,meta in inventory.items():
        im=Image.open(root/path);a=np.array(im.convert('RGBA').getchannel('A'))
        assert list(im.size)==list(meta['size']),path
        if not meta.get('opaque'):
            assert (a==0).any() and (a>240).any(),path+' alpha'
            if path.startswith(('ui/','gear/')):
                assert a[:16,:].max()==0 and a[-16:,:].max()==0 and a[:,:16].max()==0 and a[:,-16:].max()==0,path+' padding'
        checks.append({'file':path,'size':list(im.size),'alpha_min':int(a.min()),'alpha_max':int(a.max())})
    (root/'asset_manifest.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2)+'\n')
    (root/'qa.json').write_text(json.dumps({'checks':checks,'warnings':warnings,'note':'Mechanical size/alpha/padding checks do not prove anatomical or animation consistency.'},ensure_ascii=False,indent=2)+'\n')
    preview(root,[p for p in inventory if p.startswith(('ui/','gear/'))],'preview_icons.png')
    preview(root,[p for p in inventory if p.startswith('chars/')],'preview_characters.png',6)
    (root/'generation_jobs.json').write_text(json.dumps(jobs,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({'final_pngs':len(inventory),'warnings':warnings,'root':str(root)},ensure_ascii=False))

if __name__=='__main__':main()
