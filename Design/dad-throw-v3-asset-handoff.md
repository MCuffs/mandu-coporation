# 가로 던지기 v3 — P0 에셋 1차 납품 / 적용 요청

2026-11-18. 기준: `codex/dad-war-s21-presentation`의 `0dac970`, [요청서 v3](dad-throw-asset-request-v3.md). 제작 브랜치: `codex/dad-throw-assets-v3`.

납품 직전 원격 `d423b58`의 요청서 변경도 대조했다. 추가된 “상대는 아저씨, 지호 아빠는 그대로” 호칭 결정과 이번 그림·파일명은 충돌하지 않는다.

## 납품 범위와 판정

**v3 P0 24장의 PNG 후보를 제작했다.** 파일 규격 검사는 통과했지만 게임 적용·아트 최종 승인까지 끝난 상태는 아니다. v2 A~H, v3 P1 이후 묶음은 이번 납품에 포함하지 않는다.

| 묶음 | 장수 | 원본 폴더 (`Assets/DadSchoolBoss/Art/` 아래) | 판정 |
|---|---:|---|---|
| I 등장 인물 | 3 | `ThrowEntranceV1/` | **아트 재검수 필요.** 크기·발 기준점은 통과. 기존 전투 그림보다 체형이 길어졌고, 피구 인물은 요청한 낮은 출발 자세보다 상체가 높다. 미세한 색조 변화도 남아 있다. 최종본으로 자동 교체하지 않는다 |
| J 장식 | 3 | `ThrowEntranceV1/` | 연결용 후보. 반복 속도선·중앙이 빈 집중선·96px 고정 모서리 말풍선 |
| K 상점 | 12 | `ThrowShopV1/` | 연결용 후보. 강화 9종은 48px 축소 미리보기로 구분 확인. 동전·가게·구매 완료 표시 포함 |
| M 효과 | 6 | `ThrowFxV2/` | 연결용 후보. 막힘 3프레임·동전 3프레임, 고정 기준점 |

이미지 안에 대사·가격·이름을 넣지 않았다. 실제 알파 투명 PNG이며 가짜 체크무늬 배경은 없다. 각 폴더에 `manifest.json`과 `prompts.json`이 있다. 좌표는 **좌상단 원점**, bounds의 오른쪽·아래쪽은 exclusive다.

## 먼저 볼 미리보기

- [상점 9종: 큰 그림과 실제 48px](dad-throw-v3-review/shop-icons-review.jpg)
- [컷인 3명: 몸 높이 520px, 같은 발선](dad-throw-v3-review/entrance-review.jpg)
- [장식 반복·말풍선 크기 변경·효과 프레임](dad-throw-v3-review/decor-fx-review.jpg)
- [파일 QA 결과](dad-throw-v3-review/qa-results.json): 24장, 171개 검사, 0개 실패. **화풍 승인이나 실기기 검사를 뜻하지 않는다.**

## I 컷인의 해상도와 보류 사유

작은 전투 스프라이트를 확대하지 않았다. 별도로 생성한 세로 원본을 축소하고 투명 여백으로 2048×2048 캔버스에 배치했다. 최종 `standPixels=1400`, `pivotPixels=(1024,1900)`. 변환한 발 위치 오차는 1px 이하다. 머리 위 소품도 캔버스 안에 들어간다.

| 파일 | 생성 원본 | 원본 머리~발 높이 | 축소 배율 |
|---|---|---:|---:|
| `tray_entrance.png` | 887×1774 | 1767px | 0.7923 |
| `chair_entrance.png` | 887×1774 | 1413px | 0.9908 |
| `dodge_entrance.png` | 887×1774 | 1512px | 0.9259 |

가장 큰 남은 문제는 **기존의 약 3.5등신 체형과 자세 일치**다. 특히 쟁반 인물의 다리가 길다. 컷인 담당자는 이 세 장을 검토용으로 보고 기존 전투 원화와 나란히 대조한 뒤 승인하거나 다시 그려야 한다. 선 굵기와 평면 채색도 100% 화면에서 추가 확인한다. 파일 크기만 맞았다는 이유로 완성 승인하지 않는다.

## 적용 담당자에게 요청할 작업

### 1. 원본과 런타임 그림을 분리

이 브랜치는 **Art 원본·검수 자료만 추가**했다. 기존 `Resources/`, 게임 C# 및 스테이지 JSON을 수정하지 않았다. 기존 작업 브랜치에 이 커밋을 가져온 뒤 승인한 PNG만 아래 목적지에 복사한다. `sources/`의 SVG나 검수용 생성 원본은 Resources에 넣지 않는다.

| 원본 | 제안하는 런타임 목적지 | Resources 키 접두사 |
|---|---|---|
| `Art/ThrowEntranceV1/*.png` | `Resources/DadThrowEntranceV1/` | `DadThrowEntranceV1/` |
| `Art/ThrowShopV1/*.png` | `Resources/DadThrowShopV1/` | `DadThrowShopV1/` |
| `Art/ThrowFxV2/*.png` | `Resources/DadThrowFxV2/` | `DadThrowFxV2/` |

원본과 런타임 복사본에 **같은 .meta GUID를 복사하지 않는다**. 런타임 복사본에는 새 GUID를 발급한다. 원본의 maxTextureSize 2048, NPOT 유지, 알파 투명, Bilinear, Mipmap OFF 설정을 참고한다. Android 플랫폼 override가 1024로 내려가지 않는지 확인한다. 속도선만 Wrap U=Repeat, 나머지는 Clamp다. 고해상도 원본을 모두 무압축으로 상주시켰을 때의 메모리는 별도로 측정하고, Android 압축은 선 품질을 확인한 뒤 결정한다.

### 2. 컷인 연결 전에 좌표 처리 수정

기준 커밋의 `DadThrowStage.Kind.EntrancePlace()`는 **전투용 `Kind.canvas`와 `Kind.pivotY`**를 읽는다. 따라서 요청서 15절의 “entrance.resource만 바꾸기”로는 2048px 그림이 올바르게 표시되지 않는다.

`Entrance`에 전용 `canvas`, `pivotY`를 추가하고, 배치는 아래 값으로 계산한다. 구형 JSON에는 기존 Kind 값으로 fallback하도록 해 기존 그림이 깨지지 않게 한다. **전투용 Kind의 canvas·pivot은 바꾸지 않는다.**

```text
entrance.canvas = 2048
entrance.pivotX = 1024
entrance.pivotY = 1900
entrance.standPixels = 1400
k = displayedBodyHeight / entrance.standPixels
rect = (feetX - entrance.pivotX*k, feetY - entrance.pivotY*k,
        entrance.canvas*k, entrance.canvas*k)
```

쟁반/의자는 아트 승인 후 `entrance.resource`를 해당 키로 바꾼다. 피구는 새 entrance 블록과 이름·대사·힌트가 필요하다. 대사는 이번 납품에서 임의 확정하지 않았다.

### 3. 장식·상점 연결

- 컷인 레이어: 기존 거리 → 노란 띠+`entrance_band_lines` → `entrance_burst` → 인물 → `entrance_balloon` → 게임이 그리는 글자. 속도선은 X축 반복 스크롤. 집중선은 인물 몸통 뒤에 놓는다.
- 말풍선 `nineSlice`는 좌·상·우·하 **96px**. 글자는 `textSafeRect=[110,96,914,224]`를 참고해 배치한다. 일반 Texture 통째 늘리기로 꼬리를 찌그러뜨리지 않는다.
- `DadThrowPrototype.DrawShop()`의 공/신발 공용 그림 분기를 카드 ID별 `shop_*.png` 매핑으로 바꾼다. 매핑은 순서가 아니라 명시적인 ID/enum으로 한다.
- 기존 카드 순서에 대응하는 그림: 묵직한 공→pierce, 빠른 손→reload, 쌍둥이 공→twin, 돌덩이 공→heavy, 통통 공→bounce, 폭탄 신발→blast, 왕발 신발→wide, 끈끈이 신발→sticky, 신바람→flow. 가격·효과·광고·저장 로직은 이 그림 연결에서 변경하지 않는다.
- `icon_won`은 재화 옆, `icon_shop`은 상점 버튼, `mark_soldout`은 다 샀다 상태의 보조 표시에 사용한다. 텍스트의 접근성과 구매 가능/불가 구분을 유지한다.

### 4. 효과 연결

- `fx_tray_clang_0..2`: 공이 쟁반에 실제 막힌 이벤트에 1회, 총 약 0.3초. 공중에서 맞은 위치, 표시 폭 약 60 논리px. 프레임당 0.1초. pivot=(128,128).
- `fx_coin_pop_0..2`: 실제 적 처치/보상 이벤트에 1회. 기준점은 상대 머리 위. 모든 프레임 pivot=(64,108), 프레임당 0.1초. 동전은 프레임 안에서 위로 이동하므로 매 장의 내용 bounds로 다시 중앙 정렬하지 않는다. 마지막 프레임 뒤에는 숨긴다.
- 수십 개 연속 등장에 대비해 풀링·동시 표시 상한을 둔다. 애니메이션마다 재화 지급을 실행하지 않는다. 지급은 기존 모델 이벤트에서만 한다.

### 5. 적용 후 확인

1. 컷인 3명: 1280×720, 1920×1080, S21 가로 안전 영역에서 발선·말풍선 겹침·잘림·100% 선 품질 확인.
2. 상점: 9종, 구매 가능/돈 부족/최대 강화 상태, 48px 식별성 확인.
3. 쟁반 막힘 0.3초, 동전 다수 발생, 일시정지·재시작에서 잔상과 중복 보상 없는지 확인.
4. 실제 Android 빌드의 텍스처 축소·알파 테두리·메모리 확인. 이번 파일 QA로 실기기 검사를 대체하지 않는다.

## 제작 및 재현

캐릭터·강화 아이콘은 **내장 image_gen**으로 새로 생성했다(API/CLI 미사용). 선택한 원본과 지시문은 [검수 폴더](dad-throw-v3-review/)에 보존했다. 장식·간단한 UI 표식·효과는 SVG 원본을 직접 제작해 sharp 0.35.5로 정확한 크기의 PNG를 출력했다. SVG는 해당 Art 폴더의 `sources/`에 있다.

필요 환경: Node.js+sharp 0.35.5, Python+Pillow 12.3.0. 외부 sharp 경로는 `DAD_SHARP_MODULE` 환경변수로 지정할 수 있다.

```text
node Tools/build_throw_v3_vectors.cjs
python Tools/package_throw_v3_assets.py
python Tools/write_throw_v3_metas.py
python Tools/check_throw_v3_assets.py
```

검수용 생성 원본은 `Design/dad-throw-v3-review/sources/`에 있으며 패키징 스크립트는 확대를 금지한다. `generation-records.json`은 저장소 내부 상대 경로를 사용한다. Unity 창과 Play Mode는 실행하지 않았다.

## 적용 결과 (2026-11-18, 적용 담당)

사용자가 이 문서를 넘겨 적용을 맡겼다. 제작 브랜치를 `codex/dad-war-s21-presentation`에 합치고 24장을 전부 연결했다.

| 요청 | 한 것 |
|---|---|
| 1. 원본과 런타임 분리 | `Tools/install_throw_v3_runtime.py`가 PNG 24장을 `Resources/DadThrowEntranceV1`, `DadThrowShopV1`, `DadThrowFxV2`로 복사하고, 원본의 가져오기 설정에 **경로로 만든 새 GUID**를 넣은 `.meta`를 쓴다. 다시 돌려도 결과가 같다. `sources/`는 복사하지 않는다 |
| 2. 컷인 좌표 | `Entrance`에 `canvas`, `pivotY`를 더했다. 스테이지 파일에 없으면 전투용 값을 쓴다. 쟁반·의자 아저씨의 `entrance`를 2048 캔버스, (1024,1900), 키 1400으로 바꿨다 |
| 3. 장식·상점 | 띠 위에 속도선이 반복해 흐르고, 인물 뒤에서 집중선이 천천히 돈다. 대사는 96px 모서리를 지키는 아홉 조각 말풍선 안의 안전 영역에 쓴다. 상점 카드는 `DadThrowShop.IconKey(item)`으로 강화마다 자기 그림을 읽는다. 동전은 위쪽 띠·타이틀·상점에, 가게 그림은 상점 버튼에, 다 샀다 표시는 글자 옆에 붙였다 |
| 4. 효과 | 쟁반에 막힌 자리에 3프레임 0.3초. 돌려보낸 사람 머리 위에 동전 3프레임 0.3초(같은 기준점 (64,108)으로 배치). 동시에 최대 48개. 돈은 전투의 집계로만 들어오고 그림과 무관하다 |
| 5. 확인 | 창 없는 검사 413개 통과(그림 파일 존재, 좌표 계산, 배치 포함). Unity 2021.3.2f1 에디터 1920×1080에서 화면 11장을 다시 찍었고 오류 기록 0 |

**I 묶음에 대한 적용 담당의 의견:** 세 장 모두 선이 또렷하고 같은 사람으로 읽힌다. 전투 그림보다 다리가 길어 보이는 것은 맞고(쟁반 아저씨가 가장 그렇다), 피구 아저씨는 출발 자세보다 걷는 자세에 가깝다. 컷인에서는 큰 문제로 보이지 않아 쟁반·의자 두 장을 연결했다. **최종 승인은 사용자가 게임 화면을 보고 한다.** 되돌리려면 스테이지 파일의 `entrance` 다섯 줄만 예전 값으로 바꾸면 된다.

**하지 않은 것**
- 피구 아저씨 컷인: 그림은 Resources에 들어갔지만 이름·대사·힌트가 정해지지 않아 연결하지 않았다.
- Android 텍스처 압축과 메모리: 컷인 큰 그림 3장과 집중선은 무압축 2048 텍스처이고 시작할 때 전부 읽는다(무압축 기준 약 60MB). 실기기에서 재고 압축을 정해야 한다.
- 1280×720, S21 안전 영역, 실기기 확인.
