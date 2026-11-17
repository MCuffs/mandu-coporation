"""Create missing metadata for this delivery only; never alter existing metas."""
import re
import uuid
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/DadSchoolBoss/Art'
TEMPLATE=(ART/'ThrowV1/projectile_golf.png.meta').read_text()
NAMESPACE=uuid.UUID('1150e71b-0576-4bb7-ad04-236dc78d73a5')

def main():
    count=0
    for folder in ['ThrowEntranceV1','ThrowShopV1','ThrowFxV2']:
        base=ART/folder
        for p in [base,*sorted(base.rglob('*'))]:
            if p.suffix=='.meta':continue
            dest=Path(str(p)+'.meta')
            if dest.exists():continue
            guid=uuid.uuid5(NAMESPACE,p.relative_to(ROOT).as_posix()).hex
            if p.suffix=='.png':
                content=re.sub(r'guid: [a-f0-9]+','guid: '+guid,TEMPLATE,count=1)
                if p.stem=='entrance_band_lines':content=content.replace('wrapU: 1','wrapU: 0')
            else:
                importer='TextScriptImporter' if p.suffix=='.json' else 'DefaultImporter'
                content=f'fileFormatVersion: 2\nguid: {guid}\n'+('folderAsset: yes\n' if p.is_dir() else '')+f'{importer}:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
            content='\n'.join(line.rstrip() for line in content.splitlines())+'\n'
            dest.write_text(content,encoding='utf-8');count+=1
    print(f'Created {count} missing asset metadata files; existing metadata preserved.')

if __name__=='__main__':main()
