# 아빠 전쟁 — 가로 투척 에셋 요청서 v2 (대문 터치 대항전)

2026-11-17. [스토리·연출 제작안 v2](dad-throw-story-animation-v2.md)를 게임에 적용한 뒤, 그리고 [다음 스테이지 전략 v2](dad-throw-stage-strategy-v2.md)를 진행하는 데 필요한 그림과 소리를 적었다. 그림 안에는 글자를 넣지 않고, 대사·숫자·버튼 글자는 전부 게임이 그린다.

## 0. 한눈에 보기

| 묶음 | 내용 | 장수 | 순위 | 지금 게임에서의 임시 처리 |
|---|---|---:|---|---|
| A | 도전자 4명(지호 아빠 + 동네 아빠 3명): **달리기**·탈락·대문 터치 | 28 | **P0** | 전원이 예전 껌 짱 그림 2프레임(한 판에 100~160명이 같은 얼굴). 달리는 모습은 걷는 그림을 앞으로 기울여 흉내 낸다. 맞으면 같은 그림이 뒤집혀 튕겨 나감 |
| B | 효과: 대문 손바닥, 맞음, 밀림, 착지, 달리기 먼지, 그림자 | 16 | **P0** | 손바닥은 코드로 그린 네모 조합. 맞은 자리 불꽃과 발뒤꿈치 먼지는 코드로 찍은 점. 공은 떨어진 자리에서 그냥 사라짐 |
| C | 화면 아이콘 6종 | 6 | **P0** | `대문`, `남은 도전자`를 글자로 표시. 정지는 막대 두 개 |
| D | 오프닝·결말 인물을 전투 화풍으로: 기본 포즈 5, 표정 4, 동작 포즈 7, 정리 장면 1 | 17 | **P0** | 홈페이지 화풍의 전신 포즈 4장과 지호 아빠 1장을 쓰는 중. 전투의 아빠와 그림이 다름 |
| E | 전투용 아빠 연결 포즈 | 2 | P1 | 준비·당기기·던지기 3장만으로 전환 |
| F | 2장: 해 질 녘 배경 1, 성급한 아빠 7, 큰아빠 8 | 16 | P2 | 없음 |
| G | 3장: 장난감 골프채 7, 태엽 자동차 5, 밤 배경 1 | 13 | P3 | 없음 |
| H | 소리 10종 | 10 | P0~P1 | 소리 없음 |

P0 그림은 67장이다. A 묶음부터 받으면 된다.

**2026-11-17 저녁 변경.** 게임이 "도전자가 떼로 달려오는" 방향으로 바뀌었다(1-1에 100명, 1-3에 160명, 한 화면에 20~60명). 그래서 A 묶음의 걷기 4장은 **전력 질주 4장**으로, 탈락해 걸어 나가는 그림은 **허둥지둥 뛰어 돌아가는 그림**으로 내용을 바꿨고, B 묶음에 달리기 먼지 3장을 더했다. 파일 이름은 그대로다.

## 1. 화풍 기준 — 전투 화풍 (2026-11-17 사용자 결정)

게임의 모든 인물은 **전투 화풍**으로 그린다. 기준 그림은 아래 셋이다.

| 기준 그림 | 무엇의 기준인가 |
|---|---|
| `Assets/DadSchoolBoss/Resources/DadThrowV1/dad_ready.png` | 칠석(아빠)의 얼굴·옷·선 굵기·그림자 |
| `Assets/DadSchoolBoss/Resources/DadThrowScreensV2/title_wide.png` | 동네 아빠 세 명, 인물과 배경이 함께 있을 때의 밀도 |
| `Assets/DadSchoolBoss/Resources/DadV5/chars/gum/gum_walk_0.png` | 지호 아빠 |

이 결정으로 달라지는 것:

- 오프닝·결말에 지금 쓰는 그림(`Art/ThrowStoryV1/family_actors_clean.png`, `rival_challenge.png`)은 홈페이지 화풍이라 **전투 화풍으로 다시 그린다**(D 묶음). 그 전까지는 지금 그림으로 재생된다.
- 홈페이지 가족 그림(`Design/dad-throw-story-v1/homepage-family-reference.webp`)은 **이름·옷·머리 모양·서로의 키**를 확인하는 설정 자료로만 쓴다. 선과 얼굴 그리는 방식은 따르지 않는다.
- 전투의 아빠, 완료·실패 포즈, 타이틀 원화는 그대로 쓴다.
- 거실 배경(`bg_home.png`)은 인물이 아니므로 그대로 쓴다.

## 2. 공통 규칙

**형식**
- PNG. 인물·효과·아이콘은 진짜 투명 배경(체크무늬나 흰 바탕이 구워진 것은 반려). 배경과 넓은 장면은 불투명.
- 그림 안에 글자·숫자·로고·말풍선·UI를 넣지 않는다. 실제 상표나 실제 앱 화면을 그리지 않는다.
- 바닥 그림자와 바닥선을 인물 그림에 넣지 않는다(그림자는 B 묶음을 게임이 깐다).
- 여러 장을 한 시트로 생성해도 되지만 **납품은 한 장씩 자른 파일**이다. 시트로 만들 때는 칸마다 사방 8% 이상 비워 두고, 어떤 부분도 칸 경계를 넘지 않게 한다.

**화풍 (전투 화풍, 기준 `dad_ready.png`)**
- 굵고 진한 검은 외곽선. 평평한 색에 한 단계 어두운 그림자 덩어리를 얹는다. 그라데이션·광택·사진 질감·픽셀 필터 금지.
- 얼굴은 투박하게: 굵은 눈썹, 반쯤 감기거나 찌푸린 눈, 큰 코, 수염 점, 주름 몇 줄. 눈을 크고 동그랗게 그리지 않는다.
- 약 3.5등신, 어깨와 몸통이 두툼한 중년 체형. 색은 채도가 낮은 올리브·남색·갈색·크림 계열.

**인물 규칙**
- 도전자는 전원 성인 남자(동네 아빠)다. 무기·주먹질·위협 자세가 없다. 대문을 먼저 만지려고 경쟁하는 사람들이다.
- 맞은 표현은 놀람·머쓱함·휘청임까지다. 피·멍·상처·기절·눈 X자 금지.
- 아이(만수, 철수)는 전투 화면에 넣지 않는다. 오프닝·결말의 실내 장면에만 나온다.
- 수진은 주걱을 들 수 있지만 사람을 때리거나 위협하는 자세는 그리지 않는다.

**크기와 기준점 — 전투용 인물 (A, F)**
- 캔버스 768×768. 발바닥 중앙이 (384, 720). 서 있는 키(머리 끝~발바닥)는 560px(큰아빠만 600px).
- 모두 **왼쪽을 본다**(왼쪽으로 걸어 대문으로 간다). 탈락해 돌아가는 프레임만 오른쪽을 본다.
- 같은 인물의 프레임끼리 머리 높이 차이 12px 이내, 인물끼리 키 차이 3% 이내.
- 게임에서는 키 102~112로 그려진다(1080p 폰에서 약 160px). 표정은 눈썹과 입 모양 정도만 읽히므로, 네 사람이 모자·안경·머리 모양·옷 색 덩어리로 구분돼야 한다.

**파일명과 납품 위치**
- 영문 소문자와 밑줄만. 아래 표의 이름 그대로.
- 원본: `Assets/DadSchoolBoss/Art/ThrowCastV1/`(A, F), `Art/ThrowFxV1/`(B), `Art/ThrowUiV1/`(C), `Art/ThrowStoryV2/`(D), `Art/ThrowV1/`(E, 기존 아빠 그림 옆), `Art/ThrowChapter2/`·`Art/ThrowChapter3/`(F, G의 배경과 도구).
- 각 폴더에 `manifest.json`과, 생성에 쓴 지시문을 적은 `prompts.json`을 함께 둔다. 게임이 읽는 사본(`Resources/…`)은 연결 작업에서 내가 만든다.

**manifest.json에 적을 것**

```json
{
  "version": 1,
  "assets": [
    {"id": "jiho_walk_0", "file": "jiho/jiho_walk_0.png", "width": 768, "height": 768,
     "bounds": [230, 160, 540, 720], "pivotPixels": {"x": 384, "y": 720}, "facesLeft": true},
    {"id": "jiho_touch_0", "file": "jiho/jiho_touch_0.png", "width": 768, "height": 768,
     "bounds": [150, 160, 540, 720], "pivotPixels": {"x": 384, "y": 720}, "facesLeft": true,
     "handPixels": {"x": 168, "y": 395}}
  ]
}
```

- `bounds`: 불투명한 부분의 왼쪽·위·오른쪽·아래(픽셀, 왼쪽 위가 0).
- `pivotPixels`: 발바닥 중앙. `handPixels`: 손바닥이나 쥔 손의 중심(표에 "손 위치 기록"이라고 적은 그림만).

**반려 기준**: 투명이 아님 / 글자·로고가 있음 / 잘린 손발 / 손가락 수 이상 / 같은 인물인데 옷·얼굴이 다름 / 방향이 반대 / 발이 기준점에서 10px 넘게 벗어남 / 칸 경계를 넘음 / 그림자·바닥이 구워짐.

## 3. A 묶음 — 도전자 4명 (P0, 28장)

스토리에서 도전자는 "소문을 듣고 줄을 선 동네 아빠들"이다. 지금은 화면의 모든 사람이 같은 그림이라 한 사람이 복제된 것처럼 보인다. 네 사람을 섞어 내보낸다.

한 판에 100~160명이 달려오고 한 화면에 20~60명이 겹쳐 보인다. 얼굴은 거의 읽히지 않으므로 네 사람이 **머리·모자 실루엣과 옷 색 덩어리**만으로 구분돼야 한다. 달리는 프레임은 초당 약 20장으로 빠르게 돌기 때문에 네 장의 몸통 위치가 흔들리면 떨려 보인다.

> **2026-11-18 사용자 결정: 상대는 "OO 아저씨"로 부른다.** 지호 아빠는 이야기 속에서 "지호의 아빠"로 나오는 사람이라 그대로 둔다. ID와 파일 이름은 바뀌지 않는다. 이 문서의 다른 곳에 남은 "동네 아빠"라는 말은 같은 사람들을 가리킨다.

| ID | 사람 | 외형 | 기준 그림 |
|---|---|---|---|
| `jiho` | 지호 아빠 (첫 도전자) | 숱 적고 삐죽한 검은 머리, 수염 자국, 긴 코, 금색 귀고리. 주황 트레이닝 재킷(소매 흰 줄 2개), 얼룩 묻은 크림 티, 짙은 트레이닝 바지(흰 줄), 베이지 쪼리. 걷는 동안 분홍 껌 풍선을 분다(지금 그림과 같음) | `Resources/DadV5/chars/gum/gum_walk_0.png` |
| `polo` | 안경 아저씨 | 검은 곱슬머리, 둥근 안경, 크림 바탕에 갈색 가로줄 폴로 티, 벨트, 갈색 바지, 갈색 구두. 통통함 | `Resources/DadThrowScreensV2/title_wide.png` 오른쪽 무리의 왼쪽 사람 |
| `tracksuit` | 트레이닝 아저씨 | 뒤로 넘긴 짧은 검은 머리, 굵은 눈썹, 남색 트레이닝복 상하의(흰 줄 2개), 흰 운동화. 다부짐 | 같은 그림의 가운데 사람 |
| `bucket` | 벙거지 아저씨 | 베이지 벙거지 모자, 수염 자국, 회색 반팔, 자주색 조끼, 카키 건빵바지, 갈색 신발. 키 작고 둥글둥글 | 같은 그림의 오른쪽 사람 |

사람마다 아래 7장. 파일은 `<id>/<id>_<동작>_<번호>.png`.

| 파일 | 내용 | 방향 | 게임에서 쓰는 때 | 비고 |
|---|---|---|---|---|
| `<id>_walk_0.png` | **전력 질주.** 왼발을 앞으로 뻗어 땅에 닿는 순간. 몸을 앞으로 깊게 숙이고 팔을 크게 휘두른다 | 왼쪽 | 달려오는 동안 0→1→2→3 반복, 초당 약 20장(달리는 속도에 맞춰 빨라진다) | 네 장의 머리 높이가 거의 같아야 한다. 파일 이름은 `walk` 그대로 |
| `<id>_walk_1.png` | 두 발이 모두 땅에서 떨어진 순간(왼발로 박찬 직후) | 왼쪽 | 〃 | |
| `<id>_walk_2.png` | 오른발을 앞으로 뻗어 땅에 닿는 순간(0번의 반대 발) | 왼쪽 | 〃 | |
| `<id>_walk_3.png` | 두 발이 모두 땅에서 떨어진 순간(오른발로 박찬 직후) | 왼쪽 | 〃 | |
| `<id>_out_0.png` | 말랑한 공에 맞고 "어이쿠" 하며 뒤로 휘청. 눈을 질끈 감고 한 발이 들림. 상처·별 없음 | 왼쪽 | 맞은 직후 0.12초 | 별·선 같은 효과는 그리지 않는다(B 묶음) |
| `<id>_out_1.png` | 돌아서서 허둥지둥 뛰어 돌아가는 자세. 두 발이 땅에서 떨어져 있고 팔을 휘젓는다. 머쓱한 얼굴 | **오른쪽** | 맞은 뒤 0.5초: 뒤로 150 밀려나며 한 번 튀어 오르고 사라진다 | 공 하나에 12명까지 맞으므로 수십 명이 동시에 이 그림으로 나온다 |
| `<id>_touch_0.png` | 앞 팔을 어깨 높이로 쭉 뻗어 손바닥으로 대문을 짚는 순간. 의기양양한 얼굴 | 왼쪽 | 대문에 닿은 순간 0.3초 | **손 위치 기록**(뻗은 손바닥 중심) |

**지시문 초안 (사람마다 외형 문단만 바꿔 쓴다)**

```text
Use case: identity-preserve. Transparent PNG sprite sheet for a side-view mobile game, 4 columns x 2 rows, eight equal cells,
each cell a square with at least 8 percent empty margin on every side, nothing crossing a cell boundary, real alpha transparency.
Character: [JIHO'S DAD, the same man as the reference sprite: sparse spiky black hair, stubble, long nose, gold hoop earring,
orange tracksuit jacket with two white sleeve stripes, stained cream t-shirt, dark track pants with white stripe, beige flip-flops,
blowing a pink gum bubble while he walks]. Match the reference exactly in drawing style: heavy dark black outlines, flat muted
colors with one darker shadow shape, gruff middle-aged face with thick brows, half-lidded eyes and stubble dots, stocky body
about 3.5 heads tall. He is an over-competitive neighbor racing to touch a house gate, not a fighter: no fists raised, no weapons.
Same scale, same head height and same foot baseline in every cell. No ground, no cast shadow, no text, no effects.
Row 1, all FACING LEFT, a full sprint cycle, body leaning far forward, arms swinging wide: (1) left foot reaching forward and
landing, (2) both feet off the ground just after pushing off the left foot, (3) right foot reaching forward and landing,
(4) both feet off the ground just after pushing off the right foot.
Row 2: (5) FACING LEFT, just bonked by a soft toy ball, recoiling backward, eyes squeezed shut, one foot lifted, surprised not
hurt, no stars, no injury; (6) FACING RIGHT, turned around and scrambling away in a hurry, both feet off the ground, arms
flailing, sheepish face; (7) FACING LEFT, front arm stretched straight forward at shoulder height with an open palm slapping something, triumphant
grin; (8) leave empty.
```

## 4. B 묶음 — 효과 (P0, 16장)

전부 투명. 한 효과의 프레임은 같은 캔버스에서 중심이 같아야 한다. 색은 크림·노랑·먹색만 쓴다(붉은 피 색 금지).

| 파일 | 캔버스 | 내용 | 게임에서의 크기·위치 | 재생 |
|---|---|---|---|---|
| `fx_gate_palm_0.png` | 256×256 | 크림색 손바닥 자국(먹색 외곽선) + 주변에 짧은 충격선 3개 | 대문 위 약 44px | 0→1→2, 0.6초 동안 |
| `fx_gate_palm_1.png` | 256×256 | 손바닥 자국만 | 〃 | |
| `fx_gate_palm_2.png` | 256×256 | 옅어진 손바닥 자국(선이 끊김) | 〃 | |
| `fx_soft_hit_0..2.png` | 256×256 | 말랑한 공이 맞은 자리: 작은 별 하나와 짧은 선이 퍼졌다가 사라짐 | 맞은 사람 가슴 높이, 약 48px | 0.18초 |
| `fx_push_0..2.png` | 384×256 | 신발에 밀릴 때: 오른쪽으로 흐르는 가로 속도선 3~4줄과 작은 먼지. 폭발처럼 보이면 안 됨 | 밀린 무리 뒤, 약 120×80 | 0.25초 |
| `fx_land_puff_0..2.png` | 256×256 | 공·신발이 땅에 떨어진 자리의 작은 흙먼지(모래색) | 착지 지점, 약 40px | 0.2초 |
| `fx_run_dust_0..2.png` | 256×256 | 달리는 발뒤꿈치 뒤로 튀는 작은 모래 먼지 덩이. 0번이 작고 진하며 2번이 크고 옅다 | 달리는 사람의 뒷발 뒤, 약 20px. 사람마다 하나씩이라 한 화면에 수십 개 | 한 걸음에 0→1→2 |
| `shadow_soft.png` | 256×96 | 가운데가 진하고 가장자리가 사라지는 타원 그림자(검정, 가장 진한 곳 40%) | 사람 발밑, 폭 약 50 | 고정 |

```text
Transparent PNG, one row of three equal square cells with wide empty margins, real alpha. Flat Korean comic effect frames,
thick dark outline, cream and warm yellow only, no red, no text, no letters. Frame 1 strongest, frame 3 almost gone, the same
centre in all three. Subject: [a cream hand-print slapped on a surface with three short impact dashes around it].
```

## 5. C 묶음 — 화면 아이콘 (P0, 6장)

각 128×128, 투명, 같은 선 굵기(약 8px), 먹색 외곽선에 평평한 색. 24px로 줄여도 알아볼 수 있어야 한다.

| 파일 | 내용 | 쓰는 곳 |
|---|---|---|
| `icon_gate.png` | 초록 세로 살 대문 한 짝과 크림색 기둥(배경 그림의 대문과 같은 모양) | 상단 "대문" 글자 옆, 남은 터치 5칸 앞 |
| `icon_challenger.png` | 옆모습 아저씨 머리 실루엣(특정 인물 아님) | 상단 "남은 도전자" 숫자 앞 |
| `icon_pause.png` | 세로 막대 두 개 | 정지 버튼 |
| `icon_sound_on.png` | 스피커와 소리 물결 | 정지 화면의 소리 버튼 |
| `icon_sound_off.png` | 스피커와 가위표 | 〃 |
| `icon_skip.png` | 오른쪽 삼각형 두 개와 세로선 | 오프닝 건너뛰기 버튼 |

## 6. D 묶음 — 오프닝·결말 인물, 전투 화풍 (P0, 17장)

오프닝(24초)과 결말(12초)은 지금 게임에 들어가 있다. 지금은 홈페이지 화풍의 전신 포즈 4장과 지호 아빠 1장으로 재생되는데, 마지막 컷에서 전투의 아빠로 넘어갈 때 다른 사람처럼 보인다. 또 컷마다 표정이 같고, 공을 건네는 장면 같은 핵심 동작이 그림으로 보이지 않는다.

**인물 설정** (옷과 머리는 홈페이지 가족 그림에서, 그리는 방식은 전투 화풍으로)
- 칠석(아빠): `dad_ready.png`와 같은 사람. 벗겨진 머리 옆의 검은 머리, 굵은 눈썹, 큰 코, 수염 점. 올리브 조끼, 크림 러닝, 남색 바지, 흰 줄 검은 슬리퍼.
- 만수(막내, 초등학생): 검은 바가지 머리. 크림 티(파란 깃·소매 끝), 파란 바지, 흰 운동화.
- 철수(형): 가르마 탄 검은 머리. 파란 티, 짙은 바지, 흰 운동화.
- 수진(엄마): 짧은 갈색 곱슬머리. 산호색 가디건, 크림 티, 올리브 치마, 크림 신발.
- 지호 아빠: A 묶음의 `jiho`와 같은 사람. **말하는 컷에서는 껌 풍선을 불지 않는다**(입이 보여야 한다).
- **서로의 키**: 칠석 100, 철수 97, 수진 93, 만수 87. 아이들도 전투 화풍으로 그리되 어른처럼 투박한 얼굴(수염 점·깊은 주름)은 넣지 않는다.

### D-0. 기본 전신 포즈 5장 (지금 쓰는 그림의 교체)

가족 4장은 각 1024×1280, 지호 아빠는 1086×1448. 투명, 전신, 발바닥이 아래 끝에서 40px 위. 칠석 키를 1,100px로 두고 위의 키 비율을 지킨다(만수 약 960px).

| 파일 | 내용 | 방향 | 지금 쓰는 그림 |
|---|---|---|---|
| `chilsuk_stand.png` | 갑자기 정신이 든 칠석. 한 손은 허리, 한 손은 가슴 앞 주먹 | 오른쪽 | `family_actors_clean.png` 오른쪽 위 |
| `mansu_stand.png` | 심각한 척 서 있는 만수. 등 뒤로 돌린 손에 휴대폰 | 정면 약간 왼쪽 | 같은 그림 왼쪽 위 |
| `cheolsu_stop.png` | 한 손을 앞으로 뻗어 말리는 철수. 놀란 얼굴 | 왼쪽 | 같은 그림 왼쪽 아래 |
| `sujin_stand.png` | 주걱을 허리 높이에 두 손으로 쥐고 한쪽 눈썹을 올린 수진 | 정면 약간 왼쪽 | 같은 그림 오른쪽 아래 |
| `jiho_challenge.png` | 한 손은 허리, 한 손은 검지로 왼쪽(대문)을 가리키는 지호 아빠. 능글맞게 웃음 | 왼쪽 | `rival_challenge.png` |

### D-1. 표정 4장

각 1024×1024, 투명, 가슴 위 흉상. **아래쪽은 수평으로 깔끔하게 잘린다**(지금 `family_reactions_clean.png`는 아래 가장자리에 색 찌꺼기가 남아 쓰지 못했다).

| 파일 | 내용 | 쓰는 컷 |
|---|---|---|
| `react_mansu_prank.png` | 심각한 척하지만 입꼬리가 올라간 만수. 가슴 앞에 휴대폰 모서리가 살짝 보임 | 오프닝 2컷 |
| `react_chilsuk_alert.png` | 눈썹을 찌푸리고 입을 다문 칠석. 오른쪽을 봄 | 오프닝 6컷 |
| `react_mansu_panic.png` | 입을 벌리고 두 손을 든 만수("일이 커졌다") | 1-3 뒤 한마디, 결말 1컷 |
| `react_sujin_dry.png` | 한쪽 눈썹을 올린 수진. 한 손에 주걱(허리 높이), 다른 손은 검지를 듦 | 1-5 뒤 한마디, 결말 3컷 |

### D-2. 동작 포즈 7장

크기와 키 기준은 D-0과 같다.

| 파일 | 내용 | 방향 | 쓰는 컷 | 비고 |
|---|---|---|---|---|
| `mansu_handover.png` | 말랑한 흰 장난감 공을 두 손으로 내미는 만수 | 왼쪽 위를 올려다봄 | 오프닝 4컷 | 공을 손에 그린다(지름 약 90px, 딤플 없는 말랑한 공) |
| `chilsuk_take.png` | 한 손을 펴서 받으려는 칠석. 시선은 손 | 오른쪽 | 오프닝 4컷 | 공은 그리지 않는다. **손 위치 기록** |
| `chilsuk_rule.png` | 엄지로 등 뒤(왼쪽)의 대문을 가리키며 다른 손은 허리에 둔 칠석. 승부를 거는 표정 | 몸은 오른쪽, 엄지는 왼쪽 | 오프닝 4~5컷 | 한 손에 흰 공을 쥠 |
| `jiho_accept.png` | 가슴을 치며 "좋지!" 하는 지호 아빠 | 왼쪽 | 오프닝 5컷 | 1086×1448 |
| `mansu_phone_down.png` | 휴대폰을 든 손을 내리고 고개를 숙인 만수 | 정면 약간 왼쪽 | 결말 1컷 | |
| `cheolsu_broom.png` | 한숨 쉬며 빗자루를 건네는 철수 | 왼쪽 | 결말 2~3컷 | 빗자루는 손에 그린다 |
| `chilsuk_sheepish.png` | 뒷머리를 긁는 칠석("밥은?") | 오른쪽 | 결말 4컷 | |

### D-3. 정리 장면 1장

| 파일 | 크기 | 내용 |
|---|---|---|
| `ending_cleanup.png` | 1672×941, 불투명 | 해 질 녘 집 앞. `Resources/DadThrowScreensV2/street_wide.png`와 같은 집·대문·담. 가운데서 칠석과 지호 아빠가 나란히 빗자루질을 하고, 만수와 철수가 공과 슬리퍼를 주워 바구니에 담는다. 대문 앞에 수진이 팔짱을 끼고 서 있다. 뒤쪽에 동네 아빠 셋(`polo`, `tracksuit`, `bucket`)이 멋쩍게 쓰레기 봉투를 든다. 인물 크기와 밀도는 `title_wide.png`를 따른다. 아래 1/5은 대사 상자가 덮으므로 중요한 것을 두지 않는다 |

```text
Use case: identity-preserve. Single transparent full-body cutout, 1024x1280, of [MANSU, the youngest son, about ten years old:
black bowl-cut hair, cream t-shirt with blue collar and sleeve trim, blue trousers, white sneakers]. Draw him in exactly the
style of the reference game sprite of the father: heavy dark black outlines, flat muted colors with one darker shadow shape,
stocky cartoon proportions. Keep a child's smooth face, no stubble or deep wrinkles. Pose: [holding out a soft plain white toy
ball in both hands, looking up to the left]. Whole body inside the canvas with generous transparent margin, feet near the
bottom. No background, no floor, no cast shadow, no text, no speech bubble, no other people. Real alpha transparency.
```

## 7. E 묶음 — 전투용 아빠 연결 포즈 (P1, 2장)

지금 아빠는 준비·당기기·던지기 3장으로만 바뀐다. 사람이 해 봤을 때 팔이 튀어 보이면 만든다(기획서 9.1).

각 1254×1254, 투명, 오른쪽을 봄. 기준은 `Resources/DadThrowV1/dad_windup.png`와 `dad_release.png`. **발 위치와 키가 기존 세 장과 같아야 한다**(발 이동 오차 2px 이내, 머리 크기 차이 5% 이내).

| 파일 | 내용 | 게임에서 쓰는 때 | 기록할 것 |
|---|---|---|---|
| `dad_transition.png` | 젖혔던 팔이 머리 위를 지나는 중간 자세. 손은 비어 있다 | 놓은 직후 0.05초 | 발, 키 |
| `dad_recover.png` | 뻗었던 팔을 내리며 준비 자세로 돌아오는 중간 자세 | 던진 뒤 0.08초 | 발, 키 |

## 8. F 묶음 — 2장 (P2, 16장)

[전략 문서](dad-throw-stage-strategy-v2.md) 4절. 1장을 사람이 해 본 뒤에 만든다.

### F-1. 배경 1장

| 파일 | 크기 | 내용 |
|---|---|---|
| `street_wide_dusk.png` | 1672×941, 불투명 | `Resources/DadThrowScreensV2/street_wide.png`의 **해 질 녘 판**. 집·대문·담·모래 바닥·도로의 외곽선 위치는 한 픽셀도 옮기지 않는다. 하늘을 주황~분홍으로, 구름에 노을빛, 집 창문에 불빛, 전체를 따뜻하게. 사람·글자 없음 |

위치가 같아야 하는 이유: 게임은 이 그림에서 잰 숫자로 움직인다. 초록 대문 문짝 가로 138~179·세로 606~720, 대문 오른쪽 기둥 끝 가로 190, 모래 바닥 시작 세로 682, 도로 경계선 세로 813(1672×941 기준 픽셀). 이 값이 달라지면 도전자가 대문이 아닌 곳에서 멈춘다.

```text
Edit this side-view game background into the SAME scene at sunset. Do not move, resize or redraw any outline: the house, the
green gate, the long wall, the sand strip and the road edge must stay pixel-aligned with the input. Change only light and
colour: orange to pink evening sky, sunlit clouds, a warm lit window, slightly warmer and darker ground. No people, no text,
no new objects. 16:9, same size as the input.
```

> **2026-11-18 사용자 결정: F-2와 F-3은 만들지 않는다.** 빠른 상대는 피구 아저씨, 단단한 상대는 의자 아저씨로 확정했다. 그 그림은 [요청서 v3](dad-throw-asset-request-v3.md)의 I, L 묶음에 있다. 아래 내용은 기록으로만 둔다.

### F-2. 성급한 아빠 `hasty` (7장)

A 묶음과 같은 규격(768 캔버스, 발 (384,720), 키 560, 왼쪽)과 같은 전투 화풍. 마르고 다리가 긴 체형, 머리띠, 형광 연두 바람막이, 검은 반바지, 주황 러닝화. 걷는 게 아니라 **뛴다**.

| 파일 | 내용 |
|---|---|
| `hasty_walk_0..3.png` | 달리기 4장. 보폭이 일반 도전자의 1.5배, 몸을 더 숙임. 1·3번은 두 발이 땅에서 살짝 뜬다(낮은 발이 기준선에서 20px 이내) |
| `hasty_out_0.png`, `hasty_out_1.png` | A와 같은 뜻(휘청 / 돌아서 걸어 나감) |
| `hasty_touch_0.png` | 달려와 손바닥으로 대문을 짚음. 손 위치 기록 |

### F-3. 큰아빠 `big` (8장)

768 캔버스, 발 (384,720), **키 600, 몸통 폭은 일반 도전자의 1.3배**. 대머리에 콧수염, 흰 러닝셔츠, 배가 나옴, 체크 반바지, 갈색 슬리퍼. 느리고 묵직하게 걷는다. 공을 두 번 맞아야 탈락한다.

| 파일 | 내용 |
|---|---|
| `big_walk_0..3.png` | 느린 걷기 4장. 팔을 거의 흔들지 않고 배를 내밂 |
| `big_hit_0.png` | 한 번 맞고 멈칫: 배를 문지르며 한쪽 눈을 찡그림. 왼쪽을 본 채 그대로 섬 (맞은 뒤 0.3초) |
| `big_out_0.png`, `big_out_1.png` | 두 번째 맞고 휘청 / 돌아서 걸어 나감 |
| `big_touch_0.png` | 대문을 손바닥으로 짚음. 손 위치 기록 |

## 9. G 묶음 — 3장 (P3, 13장)

전략 문서 5절의 후보다. 도구 규칙이 정해진 뒤에 만든다.

| 파일 | 크기 | 내용 |
|---|---|---|
| `prop_toy_club.png` | 1024×1024 투명 | 노란 플라스틱 장난감 골프채. 손잡이가 왼쪽 아래, 헤드가 오른쪽 위. 손잡이 쥐는 점 기록 |
| `dad_swing_0..2.png` | 1254×1254 투명 | 채를 뒤로 든 자세 / 앞으로 휘두르는 중 / 휘두른 뒤. 채는 그리지 않는다(쥔 손 위치 기록). 발 위치와 키는 `dad_ready.png`와 같다 |
| `fx_swing_arc_0..2.png` | 512×256 투명 | 휘두른 자리의 초승달 모양 바람선 |
| `prop_windup_car_0..1.png` | 1024×512 투명 | 빨간 태엽 장난감 자동차 옆모습(등에 태엽 손잡이). 실제 자동차처럼 보이면 안 된다. 바퀴 회전 2장. 오른쪽을 본다 |
| `fx_car_dust_0..2.png` | 256×256 투명 | 차 뒤에 남는 먼지 |
| `street_wide_night.png` | 1672×941 불투명 | F-1과 같은 조건의 밤 판(가로등 불빛, 남색 하늘) |

## 10. H 묶음 — 소리 (10종)

WAV, 48kHz, 모노(배경음만 스테레오). 앞뒤 무음 없이 자르고, 최대 음량 −3dB. 게임에는 아직 소리가 없어, 소리 버튼도 넣지 않았다.

| 파일 | 길이 | 내용 | 순위 |
|---|---|---|---|
| `sfx_throw.wav` | 0.25초 | 가볍게 던지는 "휙" | P0 |
| `sfx_ball_bonk_a.wav`, `_b.wav` | 0.15초 | 말랑한 고무공이 몸에 맞는 "통". 높이가 조금 다른 2종. 아픈 소리가 아니다 | P0 |
| `sfx_shoe_slap.wav` | 0.2초 | 슬리퍼가 맞는 둔한 "짝" | P0 |
| `sfx_gate_slap.wav` | 0.3초 | 손바닥이 철 대문을 치는 "탕" + 짧은 떨림 | P0 |
| `sfx_button.wav` | 0.08초 | 버튼 누름 | P0 |
| `jingle_clear.wav` | 1.5초 | 완료. 밝고 짧게 | P0 |
| `jingle_retry.wav` | 1.2초 | 실패. 맥 빠지지만 우울하지 않게 | P0 |
| `sfx_land.wav` | 0.12초 | 공이 모래에 떨어지는 작은 "톡" | P1 |
| `bgm_street_loop.wav` | 60~90초 | 동네 골목 분위기의 가벼운 코믹 반복 음악. 이음매 없이 반복 | P1 |

## 11. 받은 뒤 연결 작업 (참고)

| 묶음 | 연결하면 바뀌는 것 |
|---|---|
| A | 등장표에 도전자 4종을 등록하고 무리마다 섞어 배치. 맞으면 `out_0` → `out_1`로 물러나고, 대문에 닿으면 `touch_0`을 잠깐 보인다 |
| B | 코드로 그린 손바닥을 그림으로 교체. 맞은 자리·밀린 자리·착지 자리에 효과, 발밑 그림자 |
| C | 상단의 글자 옆에 아이콘, 정지 버튼, 건너뛰기 버튼 |
| D | 오프닝·결말의 인물을 전투 화풍 그림으로 교체. 2·4·5·6컷과 결말 4컷에 표정·동작 포즈를 넣고, 결말 마지막을 정리 장면으로 |
| E | 당기기와 던지기 사이, 던지기와 준비 사이에 한 장씩 끼워 넣는다 |
| F | 2장 등장표와 배경. 큰아빠가 한 번 맞았을 때의 반응 |
| H | 소리 연결과 정지 화면의 소리 버튼 |
