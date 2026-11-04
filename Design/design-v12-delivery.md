# 디자인 개선 1차 납품

2026-11-04. [로드맵](dad-war-design-roadmap-v1.md)을 먼저 작성한 뒤 그 기준으로 원화·벡터 키트·정적 시안을 제작했다.

## 납품물

- [로드맵](dad-war-design-roadmap-v1.md): D0~D7, 공통 시각 체계, 화면별 우선순위, 첫 에셋 묶음과 검수 기준.
- [배치 시안 HTML](design-preview-v1.html) / [검수한 PNG](design-preview-v1.png): 홈·현재 전투 HUD·컴포넌트 상태 3면. 실제 폰트와 기존 아빠·재화 아이콘을 재사용했다.
- Assets/DadSchoolBoss/Art/V12Design/home-corridor-v1.png: 인물·문자 없는 홈 배경.
- Assets/DadSchoolBoss/Art/V12Design/locker-gag-v1.png: 기존 지호 아빠 디자인의 성인 상대와 찌그러진 사물함, 투명 레이어.
- Assets/DadSchoolBoss/Art/V12Design/impact-fx-v1.png: 충격·먼지·펀치 궤적·별 4종의 투명 2×2 시트.
- Assets/DadSchoolBoss/Art/V12Design/ui-kit-v1.svg 및 ui-tokens-v1.json: 버튼 4상태와 패널·칩·체력·위험 형태, 색·글꼴·간격·치수.
- [생성 프롬프트와 참조](design-v12-prompts.json), Art/V12Design/manifest.json 및 README.md: 실제 크기·셀 영역·출처·연결 기준.

## 검수

내장 image_gen을 사용했고 채택한 PNG를 프로젝트에 복사했다. 기존 아빠의 얼굴·몸을 재생성하지 않았다. 효과는 빛 번짐을 줄이는 수정 요청을 한 차례 거쳐 채택했다. 원화의 알파 밖 RGB에는 색이 남아 있을 수 있으며 실제 합성 결과와 알파를 기준으로 판단한다.

[검사 기록](design-v12-checks.json): 크기 3종, 투명도 2종, 효과 셀 경계, SVG·JSON 파싱, HTML 로컬 참조 14개를 확인했다. Chrome의 오프스크린 렌더로 1280×1080 배치 결과를 확인하고 컴포넌트 하단 잘림과 필살기 문구 줄바꿈을 보정했다.

## 적용 상태

이 납품은 원화와 디자인 시안이다. 새 에셋은 Art/V12Design에 있고 현재 Resources나 게임 코드에는 자동 연결하지 않았다. Unity PNG 임포트 메타는 준비했으나 새 UI의 실제 런타임 연결·터치·Android 가독성·성능 검수는 다음 단계다. 정적 시안의 수치와 버튼은 배치용 예시이며 게임 진행·광고를 실행하지 않는다.

같은 작업 폴더에서 다른 채팅이 변경 중인 DadSchoolPrototype·DadHomeScreens·DadOpening·검사 보고서 등의 수정은 이번 커밋에 포함하지 않는다. 에셋은 상태가 다른 코드와 독립적으로 검토할 수 있다.

## 다음 작업

홈의 정보 순서와 공통 컴포넌트를 먼저 Unity에 적용하고, 현재 전투 HUD를 이어서 적용한다. 실제 기기에서 확인한 뒤 장비·강화·결과 화면에 확장한다. 유사 3D 모델·리그와 새로운 전투 조작은 별도 제작 관문을 따른다.
