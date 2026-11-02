# 다시, 한 방 — Second Fist

2D 캐주얼 골목 액션 게임의 Windows 플레이어블 데모입니다. [최신 배포판](https://github.com/MCuffs/mandu-coporation/releases)에서 Windows ZIP을 내려받고 압축을 전부 푼 뒤 SecondFist.exe를 실행하세요. Unity 설치는 필요하지 않습니다.

## 프로젝트 열기

1. 이 저장소를 클론합니다.
2. Unity Hub에서 저장소 루트 폴더를 추가합니다.
3. **Unity 2021.3.2f1**로 엽니다.
4. `Assets/Alley/AlleyFight.unity`를 열고 Play를 누릅니다.
5. 시작 화면에서 게임 시작 또는 스테이지 선택을 누릅니다. 긴 프롤로그와 합성 음성은 현재 빌드에서 제외했습니다.

## 조작

| 입력 | 동작 |
| --- | --- |
| WASD / 방향키 | 이동 |
| 마우스를 누른 채 드래그 | 가상 스틱 이동 |
| 가까운 적 | 자동 연속 공격 |
| Space / 우측 아래 필살기 버튼 | 충전된 필살기 |
| 기술 카드 클릭 | 이번 판의 기술 강화 |
| Escape / 우측 위 일시정지 버튼 | 일시정지 |

## 현재 구현

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

## 검증과 Windows 빌드

Unity 메뉴에서 다음 명령을 실행할 수 있습니다.

- `Second Fist > Run Gameplay Checks`: 전투·움직임·보상·저장·스테이지 회귀 검증
- `Second Fist > Prepare Visual Assets`: 프레임 영역·발 피벗·알파·글꼴 검증
- `Second Fist > Build Windows`: 검증 후 Windows 빌드 생성

빌드 결과는 `Build/Windows-v7-streets/SecondFist.exe`에 생성됩니다. 실행 파일뿐 아니라 같은 폴더의 데이터와 DLL도 함께 필요합니다.

검증 기록: [전투](Design/gameplay-checks.txt), [시각 자산](Design/visual-checks.txt), [빌드](Design/build-result.txt).

Unity 캐시·로컬 설정·로그·실행 파일은 Git에 포함하지 않습니다. 소스, 아트, 글꼴, 씬 및 프로젝트 설정은 포함합니다.

## 기획과 아트

- [v7 배경·적 자산과 생성 프롬프트](Design/street-assets-v7-prompts.json) — 내장 image_gen 사용, 자산은 Assets/Alley/Resources/StageArenas.png와 StreetRoster.png에 저장
- [반복 플레이·수익화 기획](Design/arcade-v6.md)

- [현재 시각·모션 방향](Design/visual-v3.md)
- [초기 시스템 방향](Design/direction-v2.md)
- [화면 콘셉트 시안](Design/concept-v2.png) — 실제 실행 화면이 아닌 목표 시안
- [모션·아이콘 생성 프롬프트](Design/visual-v3-prompts.json)
- [초기 아트 생성 기록](Design/generated-assets.md)

아트는 내장 이미지 생성 도구로 제작했습니다. 글꼴 라이선스 원문은 [Design/Licenses](Design/Licenses)에 있으며 Windows 빌드에도 복사됩니다.

## 이전 시제품

`Assets/Scenes/FirstFight.unity`는 최초 좌우 타이밍 전투입니다. 현재 장면과 분리해 보관합니다.

## 프롤로그와 전체 화면 v4

팔룡회 보스 정민수의 배신과 죽음, 아들의 몸에서 눈뜨는 순간, 엄마의 절규와 분노, 정체 선언에서 첫 전투로 이어집니다.

- 기본 실행은 전체 화면입니다. F11로 창 모드와 전체 화면을 전환합니다.
- 프롤로그: 클릭/Space/Enter로 대사를 표시하거나 다음 컷으로 진행합니다. 자동 재생 및 건너뛰기도 지원합니다.
- 전투: 상하 HUD 패널과 회색 경계선 없이 배경이 전체 화면을 채웁니다.
- 원화 6컷에 시선 연결, 정지, 암전, 섬광, 절제된 카메라 이동을 적용한 모션 코믹입니다.
- 정민수 독백과 핵심 선언 8줄은 임시 합성 음성입니다.

[프롤로그 연출·음성 기록](Design/prologue-v4.md)

## 자연스러운 전투 움직임 v5

피격 크기 변화와 위치 튐을 보정하고, 가속·감속·공격 준비·넉백·착지·회복을 연결했습니다.
뒤로 피할 때 자동 공격이 이동을 방해하지 않으며, 적의 체격별 간격과 공격 순서를 조정했습니다.
[변경 내용과 검증](Design/natural-motion-v5.md)
