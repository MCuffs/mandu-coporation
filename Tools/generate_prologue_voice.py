"""Regenerate temporary Korean prologue narration using edge-tts.
Install separately: python -m pip install edge-tts
Only authored game dialogue is sent to the speech provider. No credentials are stored.
"""
import asyncio
from pathlib import Path
import edge_tts

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Alley/Resources/Voice/Prologue'
LINES={
    0:'나는 팔룡회의 전국구 보스, 정민수였다.',
    2:'믿었던 조직이 내 뒤통수를 쳤다. 그렇게, 나는 죽었다.',
    3:'모든 게 끝난 줄 알았다.',
    5:'눈을 뜨자, 낯선 몸이 짓밟히고 있었다. 이건, 내 아들의 몸이었다.',
    10:'내가 버린 가족이, 이렇게 살고 있었다. 아이의 절규가 내 안을 찢어놓았다.',
    11:'니들 다 뒤졌다.',
    12:'내가 누군지 아냐?',
    13:'나, 팔룡회 정민수다!'
}
async def main():
    OUT.mkdir(parents=True,exist_ok=True)
    sem=asyncio.Semaphore(2)
    async def record(index,text):
        async with sem:
            path=OUT/f'{index:02}.mp3'
            await edge_tts.Communicate(text,voice='ko-KR-HyunsuMultilingualNeural',rate='-12%' if index<11 else '-8%',pitch='-8Hz').save(str(path))
            print('Recorded',index,path.stat().st_size,flush=True)
    await asyncio.gather(*(record(i,t) for i,t in LINES.items()))
if __name__=='__main__':asyncio.run(main())
