# 아빠 전쟁

아들이 장난으로 건 "아빠 의리 테스트"에 아빠가 진심으로 반응해 학교로 뛰어가고, 다른 집 아빠들과 맞붙는 세로형 캐주얼 액션 + 아빠 키우기 게임입니다. Android 우선(Unity), 보상형 광고 중심으로 기획하고 있습니다. 현재는 출시 전 시제품입니다.

## 프로젝트 열기

1. 이 저장소를 클론하고 Unity Hub에서 루트 폴더를 추가합니다.
2. **Unity 2021.3.2f1**로 엽니다. 엔진 업그레이드 계획은 [PM 문서](Design/dad-war-pm.md) 4절에 있습니다.
3. 메뉴 `Dad School Boss > Open Prototype`(`Assets/DadSchoolBoss.unity`)을 열고 Play를 누릅니다.
4. `Mandu Corporation` 화면 → 타이틀 → 「게임 시작」을 누릅니다. 처음이면 6컷 시나리오 뒤 1-1로, 저장이 있으면 홈으로 갑니다.

## 조작

| 입력 | 동작 |
| --- | --- |
| 화면을 누르고 아래로 당겼다 놓기 | 아빠가 당긴 반대 방향으로 날아가 벽에 튕기며 다른 아빠를 들이받음 |
| 상단 「집으로」 / Escape | 전투를 그만두고 홈으로 |
| 시나리오에서 탭 / Space·Enter | 다음 컷. 「건너뛰기」·Escape는 바로 전투로 |

## 현재 구현

- 오프닝: 회사 스플래시, 키아트 타이틀, 의리 테스트 6컷 시나리오(자막·건너뛰기·다시 보기)
- 홈 하단 3탭: 출발(아빠와 사물함에 박힌 지호 아빠, 엄마 방치 보상, 스테이지 카드) · 도시락(6부위 장비, 개봉, 등급 확률 표시) · 강화(4종 강화, 출동 준비물)
- 전투(11/5부터 슬링샷, [PM 문서](Design/dad-war-pm.md) D-008): 당겨 쏘기, 벽 튕김 보너스, 연쇄와 분노 발사, 사물함 찌그러짐, 다가오는 껌·의자·쟁반·피구 아빠, 두 번 맞혀야 날아가는 큰아빠, 날린 아빠로 그로기를 만드는 층 보스
- 3층 × 20스테이지, 결과 「복도 정리 끝!」(발사/목표·최대 연쇄·벽꿍), 별 3개, 3패 이후 엄마의 응원 보정, 최대 8시간 방치 보상, 로컬 저장
- 옛 자동 펀치 전투는 비교·검사용으로 코드에 남아 있습니다(`DadSchoolPrototype.UseSlingshot = false`).

광고 SDK·결제·서버 저장은 아직 연결하지 않았습니다. 모바일 실기기 검수 전입니다.

## 검사

메뉴 `Dad School Boss` 또는 에디터 명령 파일 `Library/DadPrototypeCommand.txt`로 실행합니다.

| 명령 | 내용 | 기록 |
| --- | --- | --- |
| Run Prototype Checks (`checks`) | 에셋·import 규칙·전투 기본 | [dad-prototype-checks.txt](Design/dad-prototype-checks.txt) |
| Run Play Mode Checks (`playchecks`, Play 중) | 오프닝 흐름, 옛 전투·보상·저장, 사물함, 모션, 슬링샷 전투 연결(시작·승패 저장·성장·층 보스·홈) | [dad-prototype-playchecks.txt](Design/dad-prototype-playchecks.txt) |
| Run Polish Checks (`polishchecks`) | 경제·스테이지·쟁반·장비 | [dad-polish-checks.txt](Design/dad-polish-checks.txt) |
| `slingmain:<폴더>` (Play 중) | 홈 → 전투 → 조준·비행 → 승리·패배 결과 → 층 보스·큰아빠 → 결말 10장 캡처 | [dad-war-main-slingshot-v1.png](Design/dad-war-main-slingshot-v1.png) |
| `ui:<폴더>` (Play 중) | 오프닝·홈 3탭·옛 전투 버튼·결과 화면 16장 캡처 | — |
| 슬링샷 규칙 (`Library/DadSlingshotCommand.txt`의 `checks`) | 발사·튕김·연쇄·역할 아빠·보스·60판 생성·성장 | [dad-slingshot-checks.txt](Design/dad-slingshot-checks.txt), 봇 [dad-slingshot-mainbot.txt](Design/dad-slingshot-mainbot.txt) |

## 기획 문서

| 문서 | 맡는 내용 |
| --- | --- |
| [dad-war-roadmap.md](Design/dad-war-roadmap.md) | 개발·에셋 통합 로드맵(0~4단계, 관문, 필요 에셋) |
| [dad-war-slingshot-v1.md](Design/dad-war-slingshot-v1.md) | 시제품 B: 아빠를 당겨 쏘는 새 핵심 조작(메뉴 `Dad School Boss > Slingshot B`) |
| [dad-war-pm.md](Design/dad-war-pm.md) | 결정 기록, 2주 상세 계획, 디자인 검토, 위험, 열린 질문 |
| [dad-war-plan.md](Design/dad-war-plan.md) | 제목·시나리오·인물·표현 규칙 |
| [dad-war-product-v2.md](Design/dad-war-product-v2.md) | 조작·경제·광고·검증 지표 |
| [dad-war-design-roadmap-v1.md](Design/dad-war-design-roadmap-v1.md) | UI·아트·모션 개선 로드맵 |

---

## 보관된 시제품: 다시, 한 방 (Second Fist)

아래는 「아빠 전쟁」 이전의 골목 액션 시제품 기록입니다(릴리스 v0.7.0·v1.0.0). `Assets/Alley`의 전투 시뮬레이션(`AlleyCombat`)은 아빠 전쟁도 함께 씁니다.

하이브리드 캐주얼(골목 핀볼 액션 + 체육관 아케이드 타이쿤 + 권갑 연성) 게임입니다. [최신 배포판](https://github.com/MCuffs/mandu-coporation/releases)에서 Windows ZIP을 내려받고 압축을 전부 푼 뒤 실행하세요. Unity 설치는 필요하지 않습니다.

### 프로젝트 열기

1. 이 저장소를 클론합니다.
2. Unity Hub에서 저장소 루트 폴더를 추가합니다.
3. **Unity 2021.3.2f1**로 엽니다.
4. `Assets/Alley/AlleyFight.unity`를 열고 Play를 누릅니다.
5. 시작 화면에서 게임 시작 또는 스테이지 선택을 누릅니다. 긴 프롤로그와 합성 음성은 현재 빌드에서 제외했습니다.

### 조작

| 입력 | 동작 |
| --- | --- |
| WASD / 방향키 | 이동 |
| 마우스를 누른 채 드래그 | 가상 스틱 이동 |
| 가까운 적 | 자동 연속 공격 |
| Space / 우측 아래 필살기 버튼 | 충전된 필살기 |
| 기술 카드 클릭 | 이번 판의 기술 강화 |
| Escape / 우측 위 일시정지 버튼 | 일시정지 |

### 현재 구현

- 이동 가속·감속, 자동 근접 공격, 넉백, 벽 충돌, 적 사이의 연쇄 충돌
- 복서, 긴 리치의 킥복서, 예고 후 돌진하는 중량급, 각 스테이지의 보스
- 벽꿍 / 충돌 전파 / 연쇄 기절 기술 조합
- 주인공 걷기·공격 프레임과 적 걷기, 발 접지 보정, 피격·제압 연출
- 타격 멈춤, 흔들림, 파편, 필살기 효과 및 생성 효과음
- 한글 글꼴을 포함한 HUD, 기술 카드, 시작·결과·스테이지 선택·일시정지 화면
- 학교 뒷골목 / 네온 야시장 / 노을 옥상 / 항만 물류창고의 서로 다른 배경과 적 구성
- 12 / 16 / 20 / 24명 제압 후 보스 등장, 승리하면 다음 스테이지 해금
- 코인·시작 기술·최고 기록·스테이지 진행 로컬 저장

현재는 상업 서비스 전 단계의 데모입니다. 광고·결제·온라인 순위·서버 저장은 연결하지 않았습니다. 모션은 좌우 기준의 2D 프레임 방식이며, 모바일 터치는 실기기에서 아직 검증하지 않았습니다.

### 검증과 Windows 빌드

Unity 메뉴에서 다음 명령을 실행할 수 있습니다.

- `Second Fist > Run Gameplay Checks`: 전투·움직임·보상·저장·스테이지 회귀 검증
- `Second Fist > Prepare Visual Assets`: 프레임 영역·발 피벗·알파·글꼴 검증
- `Second Fist > Build Windows`: 검증 후 Windows 빌드 생성

빌드 결과는 `Build/Windows-v7-streets/SecondFist.exe`에 생성됩니다. 실행 파일뿐 아니라 같은 폴더의 데이터와 DLL도 함께 필요합니다.

검증 기록: [전투](Design/gameplay-checks.txt), [시각 자산](Design/visual-checks.txt), [빌드](Design/build-result.txt).

Unity 캐시·로컬 설정·로그·실행 파일은 Git에 포함하지 않습니다. 소스, 아트, 글꼴, 씬 및 프로젝트 설정은 포함합니다.

### 기획과 아트

- [v7 배경·적 자산과 생성 프롬프트](Design/street-assets-v7-prompts.json) — 내장 image_gen 사용, 자산은 Assets/Alley/Resources/StageArenas.png와 StreetRoster.png에 저장
- [반복 플레이·수익화 기획](Design/arcade-v6.md)

- [현재 시각·모션 방향](Design/visual-v3.md)
- [초기 시스템 방향](Design/direction-v2.md)
- [화면 콘셉트 시안](Design/concept-v2.png) — 실제 실행 화면이 아닌 목표 시안
- [모션·아이콘 생성 프롬프트](Design/visual-v3-prompts.json)
- [초기 아트 생성 기록](Design/generated-assets.md)

아트는 내장 이미지 생성 도구로 제작했습니다. 글꼴 라이선스 원문은 [Design/Licenses](Design/Licenses)에 있으며 Windows 빌드에도 복사됩니다.

### 이전 시제품

`Assets/Scenes/FirstFight.unity`는 최초 좌우 타이밍 전투입니다. 현재 장면과 분리해 보관합니다.

### 프롤로그와 전체 화면 v4

팔룡회 보스 정민수의 배신과 죽음, 아들의 몸에서 눈뜨는 순간, 엄마의 절규와 분노, 정체 선언에서 첫 전투로 이어집니다.

- 기본 실행은 전체 화면입니다. F11로 창 모드와 전체 화면을 전환합니다.
- 프롤로그: 클릭/Space/Enter로 대사를 표시하거나 다음 컷으로 진행합니다. 자동 재생 및 건너뛰기도 지원합니다.
- 전투: 상하 HUD 패널과 회색 경계선 없이 배경이 전체 화면을 채웁니다.
- 원화 6컷에 시선 연결, 정지, 암전, 섬광, 절제된 카메라 이동을 적용한 모션 코믹입니다.
- 정민수 독백과 핵심 선언 8줄은 임시 합성 음성입니다.

[프롤로그 연출·음성 기록](Design/prologue-v4.md)

### 자연스러운 전투 움직임 v5

피격 크기 변화와 위치 튐을 보정하고, 가속·감속·공격 준비·넉백·착지·회복을 연결했습니다.
뒤로 피할 때 자동 공격이 이동을 방해하지 않으며, 적의 체격별 간격과 공격 순서를 조정했습니다.
[변경 내용과 검증](Design/natural-motion-v5.md)
