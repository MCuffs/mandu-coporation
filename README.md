# 두 번째 주먹 — Second Fist

아버지의 주먹으로 아들의 삶을 되찾는 2D 캐주얼 골목 액션 게임의 Unity 시제품입니다.

## 프로젝트 열기

1. 이 저장소를 클론합니다.
2. Unity Hub에서 저장소 루트 폴더를 추가합니다.
3. **Unity 2021.3.2f1**로 엽니다.
4. `Assets/Alley/AlleyFight.unity`를 열고 Play를 누릅니다.
5. **싸움 시작** 버튼을 누릅니다.

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
- 일반 적, 예고 후 돌진하는 덩치 큰 적, 골목 보스
- 벽꿍 / 충돌 전파 / 연쇄 기절 기술 조합
- 주인공 걷기·공격 프레임과 적 걷기, 발 접지 보정, 피격·제압 연출
- 타격 멈춤, 흔들림, 파편, 필살기 효과 및 생성 효과음
- 한글 글꼴을 포함한 HUD, 기술 카드, 시작·결과·지도·일시정지 화면
- 18명 이상 제압 후 보스 등장, 승리·패배·재시작과 다음 구역 진행

현재는 핵심 전투를 검증하는 시제품입니다. 다음 구역은 같은 전투장에 이름과 난도가 달라집니다. 모션은 좌우 기준의 2D 프레임 방식이며, 모바일 터치는 실기기에서 아직 검증하지 않았습니다.

## 검증과 Windows 빌드

Unity 메뉴에서 다음 명령을 실행할 수 있습니다.

- `Second Fist > Run Gameplay Checks`: 전투 모델 검증 15개
- `Second Fist > Prepare Visual Assets`: 프레임 영역·발 피벗·알파·글꼴 검증
- `Second Fist > Build Windows`: 검증 후 Windows 빌드 생성

빌드 결과는 `Build/Windows-v3-polished/SecondFist.exe`에 생성됩니다. 실행 파일뿐 아니라 같은 폴더의 데이터와 DLL도 함께 필요합니다.

검증 기록: [전투](Design/gameplay-checks.txt), [시각 자산](Design/visual-checks.txt), [빌드](Design/build-result.txt).

Unity 캐시·로컬 설정·로그·실행 파일은 Git에 포함하지 않습니다. 소스, 아트, 글꼴, 씬 및 프로젝트 설정은 포함합니다.

## 기획과 아트

- [현재 시각·모션 방향](Design/visual-v3.md)
- [초기 시스템 방향](Design/direction-v2.md)
- [화면 콘셉트 시안](Design/concept-v2.png) — 실제 실행 화면이 아닌 목표 시안
- [모션·아이콘 생성 프롬프트](Design/visual-v3-prompts.json)
- [초기 아트 생성 기록](Design/generated-assets.md)

아트는 내장 이미지 생성 도구로 제작했습니다. 글꼴 라이선스 원문은 [Design/Licenses](Design/Licenses)에 있으며 Windows 빌드에도 복사됩니다.

## 이전 시제품

`Assets/Scenes/FirstFight.unity`는 최초 좌우 타이밍 전투입니다. 현재 장면과 분리해 보관합니다.
