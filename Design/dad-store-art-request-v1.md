# 아빠 전쟁 — 스토어 그림 요청서 v1

2026-11-18. 사용자 결정: 임시본으로 올리지 않고 **새 그림을 기다린다.**
구글 플레이 스토어 등록정보가 그림 두 장(앱 아이콘, 대표 그림) 때문에 멈춰 있다. 이 두 장이 오면 등록정보를 마치고 공개 출시(프로덕션) 절차로 넘어갈 수 있다([준비 목록](release-prep-2026-11-18.md) 11절).

[에셋 요청서 v3](dad-throw-asset-request-v3.md) 14절(S 묶음)을 이 문서가 대신한다. 파일 이름은 같고, 규격과 안전 영역, 금지 사항을 더 적었다.

## 0. 한눈에 보기

| 묶음 | 파일 | 규격 | 순위 | 없으면 |
|---|---|---|---|---|
| SA | `store_icon_512.png` | 512×512, 32비트 PNG, 1 MB 이하 | **P0** | 스토어 등록정보를 저장할 수 없다 |
| SB | `store_feature.png` | 1024×500, 투명 없음(JPEG 또는 24비트 PNG) | **P0** | 스토어 등록정보를 저장할 수 없다 |
| SA | `store_icon_fg.png`, `store_icon_bg.png` | 각 1024×1024 | P1 | 요즘 Android에서 아이콘이 흰 바탕 안에 작게 들어간 모양으로 보일 수 있다 |
| SC | `logo_title_ko.png` | 가로 2048, 투명 | P1 | 대표 그림에 제목이 없다. 타이틀 화면은 지금처럼 글꼴로 쓴다 |
| SD | `store_shot_frame.png` | 1920×1080, 투명 | P2 | 스크린샷을 지금처럼 게임 화면 그대로 쓴다 |

**P0 두 장만 와도 출시 절차가 풀린다.** 합계 6장.

## 1. 기준으로 삼을 그림 (이미 저장소에 있다)

| 무엇 | 어디 | 쓰임 |
|---|---|---|
| 지금 빌드에 들어간 앱 아이콘 | `Assets/DadSchoolBoss/Art/V5/app_icon.png` (1024×1024) | 칠석의 얼굴, 표정, 파란 방사형 배경. **같은 사람, 같은 분위기**여야 한다 |
| 타이틀 키아트 | `Assets/DadSchoolBoss/Art/ThrowV1/title_keyart.png` (1672×941) | 대문 앞에서 공을 던지는 칠석과 움츠러든 아저씨 셋. 대표 그림의 구도 기준 |
| 싸움 화면 | [`dad-throw-shop-captures-2026-11-17/shop-10-…png`](dad-throw-shop-captures-2026-11-17/shop-10-stage-1-4-tray-stops-the-ball.png) | 떼로 몰려오는 느낌, 배경의 색 |
| 전투 인물 | `Resources/DadV5/chars/` 아래 쟁반·의자·피구 아저씨 | 상대의 얼굴과 옷 |
| 공과 신발 | `Art/ThrowV1/projectile_golf.png`, `projectile_shoe.png` | 손에 든 물건의 모양 |
| 임시본(쓰지 않기로 함) | [`store-art-provisional/`](store-art-provisional/) | 크기와 잘리는 범위를 보는 참고용 |

## 2. 공통 규칙

v2 2절과 v3 3절(형식, 전투 화풍, 인물 규칙)을 그대로 따른다. 스토어 그림에서 더하는 것:

- **글자를 그리지 않는다.** 예외는 `logo_title_ko.png` 하나다. 아이콘과 대표 그림에는 글자, 숫자, 말풍선의 대사를 넣지 않는다.
- **홍보 문구와 표시를 넣지 않는다.** "무료", "1위", "신작", 별점, 가격, 할인, 스토어 배지, 휴대폰 틀. 구글 플레이가 등록정보에서 금지하는 것들이다.
- **맞은 표현은 놀람과 머쓱함까지.** 피, 멍, 기절, 눈 X자는 그리지 않는다. 콘텐츠 등급 설문에 "선혈 없음"으로 답했다.
- **세 줄 무늬를 그리지 않는다.** 지금 신발 그림과 키아트의 운동복에는 흰 줄이 세 개 있다. 세 줄 무늬는 특정 스포츠 브랜드의 상표로 알려져 있어 스토어 그림에서는 분쟁의 소지가 있다(법률 판단은 아니다). 스토어 그림의 슬리퍼와 운동복은 **두 줄**로 그리거나 줄 없이 그린다. 게임 안 그림을 바꿀지는 5절의 결정이다.
- **실제 상표, 실제 사람을 닮은 얼굴을 넣지 않는다.**
- 어린이를 겨냥한 그림으로 보이지 않게 한다. 타겟층을 만 18세 이상으로 신고했다.

## 3. 묶음별 요청

### SA — 앱 아이콘 (3장)

| 파일 | 규격 | 내용 |
|---|---|---|
| `store_icon_512.png` | 512×512, 32비트 PNG, 1 MB 이하. **네모 그대로**(둥근 모서리와 그림자를 넣지 않는다. 스토어가 모서리를 둥글게 자른다) | 칠석의 찌푸린 얼굴을 크게, 손에 든 물건 하나(5절 결정 1). 배경은 지금 아이콘과 같은 파란 방사형. 얼굴과 물건은 **가운데 80%(410×410) 안**에 둔다. 48px로 줄여도 얼굴과 물건이 구분돼야 한다 |
| `store_icon_fg.png` | 1024×1024, 투명 | 적응형 아이콘의 앞층: 얼굴과 물건만. **가운데 지름 620px 원 안**에 다 들어와야 한다(기기마다 원, 둥근 네모로 잘린다). 배경은 그리지 않는다 |
| `store_icon_bg.png` | 1024×1024, 불투명 | 적응형 아이콘의 뒤층: 파란 방사형 무늬만. 가장자리까지 꽉 채운다. 인물과 물건은 없다 |

- 세 장은 **같은 그림**이어야 한다. 512짜리는 앞층과 뒤층을 겹친 것과 같은 모습이다.
- 지금 아이콘에서 바꾸는 것은 손에 든 물건과 줄무늬뿐이다. 얼굴은 그대로 둔다. 휴대폰에 이미 깔린 아이콘과 딴 사람처럼 보이면 안 된다.

```
App icon, 512x512 square, full bleed, no rounded corners, no drop shadow, no text.
Close-up of the same middle-aged Korean man as in app_icon.png: balding with curly side hair, heavy frown,
stubble, olive vest over a white undershirt. He holds up one golf ball in his right fist, ready to throw.
Bold dark ink outline, flat muted colors, one darker shadow shape per area, no gradients, no gloss.
Background: blue radial burst as in the current icon. Keep the face and the ball inside the central 80%.
Must read clearly at 48 px.
```

### SB — 대표 그림 (1장)

| 파일 | 규격 | 내용 |
|---|---|---|
| `store_feature.png` | 1024×500, 투명 없음, 15 MB 이하 | 왼쪽에 대문 앞에서 공을 던지는 칠석, 오른쪽에 떼로 몰려오는 아저씨들(쟁반을 든 사람, 의자를 든 사람이 섞여 있다), 그 사이로 날아가는 골프공. 배경은 동네 골목과 하늘 |

- **구도:** 키아트(`title_keyart.png`)를 가로로 더 넓힌 모습이다. 키아트는 16:9이고 대표 그림은 약 2:1이라, 위아래를 자른 느낌으로 잡는다.
- **로고 자리:** 가운데 위쪽 **(262, 40)에서 가로 500 × 세로 190**을 하늘로 비워 둔다. 로고(`logo_title_ko.png`)는 따로 얹는다. 로고가 없어도 그림이 비어 보이지 않게 구름 한두 점은 둔다.
- **안전 영역:** 얼굴, 공, 로고 자리는 가장자리에서 **가로 80px, 세로 50px 안쪽**에 둔다. 스토어가 화면에 따라 가장자리를 자르거나 재생 버튼을 가운데에 얹는다.
- 오른쪽 무리는 세 명이 아니라 **많은 사람**으로 보여야 한다(이 게임의 첫인상이 "떼"다). 뒤쪽 사람은 겹쳐서 단순하게 그려도 된다.

```
Wide promotional illustration, 1024x500, opaque, no text, no logos, no UI.
Left third: the hero from title_keyart.png in front of his green gate, mid-throw, a golf ball leaving his hand
with a short motion trail. Right half: a dense crowd of annoyed middle-aged neighbours rushing toward him,
some holding steel lunch trays as shields, some holding folding chairs overhead; front row surprised by the ball.
Korean residential alley, tiled roofs, pale wall, warm cream sky. Leave clear sky at the top centre
(about 500x190 px) for a title logo. Bold ink outlines, flat muted colors, no gradients. No blood, no injuries.
No three-stripe patterns on slippers or tracksuits (use two stripes or none).
```

### SC — 제목 로고 (1장)

| 파일 | 규격 | 내용 |
|---|---|---|
| `logo_title_ko.png` | 가로 2048(세로는 글자에 맞춰 600~800), 투명 | 「아빠 전쟁」 네 글자. 굵고 둥근 노란 글자에 먹색 테두리, 아래에 짙은 그림자 한 겹. 타이틀 화면의 글자 느낌 |

- **이 문서에서 글자를 그리는 유일한 그림이다.** 글자는 정확히 「아빠 전쟁」이어야 한다(띄어쓰기 한 칸). 받침과 획이 틀리면 반려한다.
- 부제는 넣지 않는다(5절 결정 3이 정해지면 따로 요청한다).
- 대표 그림 위에 가로 500으로 줄여 얹었을 때 읽혀야 한다.

### SD — 스크린샷 테두리 (1장, 선택)

| 파일 | 규격 | 내용 |
|---|---|---|
| `store_shot_frame.png` | 1920×1080, 투명 | 스크린샷 위에 얹는 위아래 띠 장식. 가운데 1920×860은 완전히 투명하게 비운다. 글자는 없다 |

없어도 된다. 지금 올린 스크린샷 8장은 게임 화면 그대로다.

## 4. 납품과 그 뒤

- **납품 위치:** `Assets/DadSchoolBoss/Art/ThrowStoreV1/`. `manifest.json`(파일, 크기, 아이콘 앞층의 안전 원 지름)과 `prompts.json`(생성에 쓴 지시문)을 같이 둔다.
- **받은 뒤 PM이 하는 일:**
  1. 규격 검사(크기, 투명 여부, 용량, 안전 영역, 글자 유무).
  2. `store_icon_512.png`와 `store_feature.png`를 콘솔의 스토어 등록정보에 올리고 저장한다. 로고가 왔으면 대표 그림에 얹은 판을 올린다.
  3. 앞층·뒤층이 왔으면 Android 빌드의 적응형 아이콘으로 연결하고, 기본 아이콘도 새 그림으로 바꾼다(다음 번들부터).
  4. 로고가 왔으면 타이틀 화면에 연결할지 사용자에게 묻는다.

## 5. 사용자가 정할 것

| # | 무엇 | PM 추천 | 까닭 |
|---|---|---|---|
| 1 | 아이콘에서 손에 든 물건 | **골프공** | 이 게임에서 처음 던지는 것이 골프공이다. 슬리퍼를 빼면 줄무늬 문제도 아이콘에서는 사라진다. 지금 아이콘은 슬리퍼다 |
| 2 | 게임 안 신발과 운동복의 세 줄 무늬 | 스토어 그림은 두 줄로(이 문서의 규칙). 게임 안 그림은 다음 그림 묶음에서 같이 바꾼다 | 스토어에 보이는 그림이 먼저 문제가 된다. 게임 안 그림까지 바꾸려면 신발 그림과 인물 몇 장을 다시 그려야 한다 |
| 3 | 스토어 앱 이름의 부제("대문을 지켜라") | 정해질 때까지 로고는 「아빠 전쟁」만 | 로고는 글자를 그리는 그림이라 나중에 바꾸면 다시 그려야 한다 |

정해지지 않아도 P0 두 장은 추천안대로 그리면 된다.

## 6. 반려 기준

- 크기나 형식이 다르다(대표 그림에 투명이 있다, 아이콘이 512×512가 아니다, 용량 초과).
- 아이콘과 대표 그림에 글자, 숫자, 홍보 표시가 있다.
- 칠석이 지금 아이콘·키아트와 다른 사람으로 보인다.
- 안전 영역 밖에 얼굴, 공, 로고 자리가 걸린다.
- 세 줄 무늬, 실제 상표, 피나 상처가 보인다.
- 아이콘을 48px로 줄였을 때 얼굴과 물건이 뭉개진다.
- 로고의 글자가 「아빠 전쟁」과 다르다.
