# 생성 아트 기록

내장 image_gen 도구로 생성했습니다. 프로젝트가 직접 사용하는 원본은 아래 두 PNG입니다.
- Assets/Alley/Resources/FighterAtlas.png: 1536×1024 RGBA, 4열×2행 캐릭터 아틀라스. 실제 알파 채널 확인 완료.
- Assets/Alley/Resources/AlleyBackground.png: 세로형 골목 배경.
- Design/concept-v2.png: 앞서 검토한 화면 시안이며 실행 화면이 아닙니다.

## FighterAtlas 생성 프롬프트
Use case: stylized-concept. Asset type: PRODUCTION 2D GAME SPRITE ATLAS, not a mockup. Create an exactly aligned 4 columns by 2 rows sheet, 8 equal rectangular cells, landscape 1536 by 1024 composition. Fully transparent background, no shadows beyond each character's feet, NO text, NO grid lines, NO UI. Each sprite stays completely within its cell, generous 12% transparent padding on every side, ground baseline identical per row. Orthographic front three-quarter top-down beat-em-up view, compact charming tough 2.5-head-tall characters, large expressive heads, short sturdy legs. Hand-drawn mobile casual game, bold dark navy outlines, simple clean cel shading, strong silhouettes, visually readable at 100 pixels tall, warm coral and cream palette. All characters face screen-right at three-quarter angle. Whole body including feet visible. No weapons, no blood, no licensed characters.
EXACT CELL LAYOUT:
TOP ROW 4 poses of SAME HERO: Korean black-haired young adult, swept messy black hair, white open-collar rolled-sleeve school shirt with tiny red tie hanging loose, navy trousers, white sneakers, confident older fighter expression. Cell1 idle fighting stance both fists up. Cell2 powerful straight right punch arm fully extended to right, torso twisted. Cell3 walking stride left leg forward. Cell4 walking stride right leg forward. Keep face, proportions and clothing identical.
BOTTOM ROW: Cell1 orange-jacket skinny street thug, white undershirt, dark trousers, short red-brown hair, aggressive idle fists up. Cell2 SAME orange-jacket thug recoiling backward from impact, arms flailing, feet still near baseline. Cell3 stocky larger bruiser in purple track jacket, black pants, buzzcut and thick eyebrows, idle fists up. Cell4 SAME purple bruiser reeling backward from punch, stunned expression.
Sprite sheet only. No scenery, no circles, no labels. Exact uniform grid, clear separation. Transparent background is mandatory.

## AlleyBackground 생성 프롬프트
Use case: stylized-concept. Asset type: production 2D top-down mobile game arena background, NOT screenshot, NO characters, NO UI, NO text overlays. Portrait image. Draw an appealing small Korean neighborhood school back alley as a playable action arena, orthographic near-top-down view (camera 65 degrees downward). Thick dark navy outlines, warm hand-painted clean cel shading, polished casual mobile game environment. The central 75% of the image width and central 75% height MUST be unobstructed broad flat warm grey-beige paving stone ground for walking and fighting. This is a wide open rectangular alley courtyard, not a narrow corridor or perspective vanishing-point street. Buildings and props frame ONLY far left and far right edges and far top, leaving generous open playable ground. Left edge brick wall and a closed teal shop shutter, tiny wall-mounted AC and pipes, right edge warm brick wall with orange shutter, bins and stacked crates tucked to edge. Top edge school boundary fence, trees beyond it and small distant rooftops. Bottom edge is open pavement entrance. Subtle floor cracks, drain cover at edge, a few leaves, tiny mint and burnt orange accent details. Crisp readable ground, no strong cast shadows across arena center. All objects stay near perimeter. No people, no enemies, no weapons, no cars, no bottom foreground buildings occluding ground. Visually coordinate with a cute chibi Korean street brawler game. Original assets only. No logos, no watermark, no writing. The whole image is ground-plane scene art, no phone or border.

## 현재 아트 한계
한 장의 아틀라스를 코드에서 분할하여 사용합니다. 주인공의 주먹 끝처럼 셀 경계에 가까운 부분은 별도 원화 정리 시 개선할 수 있습니다.
맵과 캐릭터는 전투 시제품용입니다. 방향별 추가 프레임과 캐릭터별 고유 보스 원화는 아직 없습니다.
