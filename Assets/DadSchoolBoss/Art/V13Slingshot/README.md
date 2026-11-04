# V13 · 당겨 쏘는 아빠 에셋

P0·P1용 1차 납품: **PNG 14개 + WAV 4개**. 원본 11개는 _sources에 별도 보관했다(최초 생성 9개·수정 2개). A-B01·A-B03의 수정본을 준비본에 사용했다. 본 게임이나 시제품 B의 코드·Resources는 바꾸지 않았다.

| 요청 ID | 준비된 파일 | 규격 |
| --- | --- | --- |
| A-B01 | bg/corridor_floor1_long.png | 1080×2340 RGB |
| A-B02 | chars/dad_pull_0.png | 768×768 RGBA |
| A-B03 | chars/dad_flight_0.png, dad_flight_1.png | 각각 768×768 RGBA |
| A-B04 | chars/dad_bounce_0.png | 768×768 RGBA |
| A-B05 | chars/dad_land_0.png | 768×768 RGBA |
| A-B06 | fx/wind.png, swoosh.png | 각각 512×512 RGBA |
| A-B07 | fx/rage_0.png, rage_1.png | 각각 512×512 RGBA, 중앙 투명 |
| A-B08 | ui/drag_hand.png | 256×256 RGBA |
| A-B09 | ui/wall_launch.png, chain_launch.png, rage_launch.png | 각각 256×256 RGBA |
| A-B13 | audio/sfx_pull_tension.wav, sfx_launch_whoosh.wav, sfx_wall_boing.wav, sfx_landing_skid.wav | 44.1kHz·PCM16·모노 |

## 배경 연결

arena-guide.json의 픽셀 좌표가 기준이다. 정면 충돌선은 왼쪽 x=103, 오른쪽 x=976의 수직선이다. 그림 위에 경계를 겹쳐 검수하고 캐릭터 반경을 안쪽으로 적용한다. 좌표를 현재 시제품에 자동으로 바꾸지는 않았다.

9:16은 [0,210,1080,1920]을 잘라 사용한다. 9:19.5는 전체를 쓴다. 문은 수정 원화에서 아래로 내려 짧은 화면에서도 남는다. 사물함은 양쪽 각각 네 구간이며 창문 구간에는 사물함 찌그러짐을 붙이지 않는다.

## 캐릭터와 피벗

원화의 얼굴·배·조끼·슬리퍼는 기존 아빠 기준이다. 기존 v7 준비 스크립트와 같은 방식으로 원본을 보존하고, 선택 배율의 등방 축소·투명 캔버스 배치로 준비했다. 웅크림을 서 있는 키로 늘이지 않았다.

- 지상: 768 캔버스, 발 피벗 (384,720), Unity 정규화 (0.5,0.0625).
- 비행: 같은 768 캔버스에 몸 중심 배치, **회전 피벗 (384,384), Unity (0.5,0.5)**. 기존 발 피벗을 그대로 회전에 쓰면 아빠가 크게 휘돈다.
- 벽 튕김: 등·어깨가 왼쪽 벽에 닿는 기준이다. 반대 벽은 반전하고 manifest의 back_contact 값을 벽 앞면에 맞춘다.
- 자세마다 source_bounds·uniform_scale·placement·pivot을 manifest에 기록했다. 비행 0→1은 연속 프레임 애니메이션이 아니라 두 가지 변형 포즈다. 짧게 번갈아 쓰되 접지·회전 보간은 실제 게임에서 검수한다.

## 효과와 안내

wind·swoosh는 비행 속도에 따라 길이·불투명도를 조정한다. rage_0·rage_1은 아빠 뒤에 두며, 가운데가 투명해 몸을 가리지 않는다. 이미지에는 연쇄 숫자·당김 방향·화살표·조준선·라벨을 넣지 않았다. drag_hand는 코드 화살표를 따라 이동시킨다.

특수 발사 아이콘은 벽꿍형 / 연쇄형 / 폭주형 순이다. 라벨은 런타임에 별도로 표시한다.

## 소리와 출처

Tools/generate_dad_b_sfx.py가 외부 샘플 없이 절차 합성했다. 별도 상용 샘플 라이선스가 없다. 이용 조건은 이 프로젝트의 배포 조건을 따른다. 당김은 반복 재생하며 세기에 따라 pitch 0.8~1.8을 매핑하고, 나머지는 발사·벽 충돌·착지 시점에 한 번씩 재생한다. 실제 기기에서 다른 효과와 섞어 최종 음량을 맞춰야 한다.

PNG는 내장 image_gen으로 생성했다. 프롬프트·참조·수정 요청은 Design/dad-b-v13-generation.json, 규격 준비 코드는 Tools/prepare_dad_b_assets.py에 있다. 생성 원본은 지우거나 덮어쓰지 않았다.

## 검수·상태

Design/dad-b-v13-preview.html에서 크롭·배치·회전 중심·모든 준비본과 소리를 볼 수 있다. PNG 미리보기도 함께 제공한다. 자동 검사는 Design/dad-b-v13-checks.json을 참고한다. 실제 Unity 임포트·런타임 연결·Android 성능 검수는 후속 단계다.

P2 A-B10·11·12·14(큰아빠 표시, 카트·매트, 2·3층, 1-1 진입 전환)는 G1 뒤 본제작 묶음으로 남겼다.
