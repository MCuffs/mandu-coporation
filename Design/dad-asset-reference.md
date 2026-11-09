# 아빠 전쟁 — 디자인 작업용 에셋 참고 목록

**현재 제목과 시나리오는 [아빠 전쟁 v10](dad-war-scenario-v10.md)이 우선한다.** 이전 도시락 배달 도입과 학교 점거 설정을 새 제작에 사용하지 않는다.

캐릭터·UI·연출을 새로 만들거나 연결할 때 아래 문서와 실제 PNG를 먼저 확인한다. 기존 에셋을 재활용할 수 있는지 검토한 뒤 부족한 것만 추가한다.

| 범위 | 기준 문서 | 원본 위치 |
|---|---|---|
| 기본 캐릭터·배경·UI·장비 | [v5 요청 규격](dad-asset-request-v5.md) | `Assets/DadSchoolBoss/Resources/DadV5/` |
| 도시락 개봉·장비 카드·사물함 파손 | [v6 적용 지침](dad-reward-assets-v6.md) | `Assets/DadSchoolBoss/Art/V6Rewards/` |
| 걷기 중간·출발·정지·벽 충돌·주저앉기 | [v7 움직임 기준](dad-motion-assets-v7.md) | `Assets/DadSchoolBoss/Art/V7Motion/` |

## 우선 원칙

- v5 캐릭터의 얼굴·체격·의상·굵은 외곽선을 유지한다.
- 추가 프레임은 기존 프레임과 연결해 본 뒤 채택한다. 파일 수만 늘려 부드러움을 판단하지 않는다.
- 글자·수치·수집 상태는 Unity에서 표시한다. 효과음·타격 정지·카메라 흔들림·보행 속도도 재생 구현과 함께 조정한다.
- 원본 보관 폴더와 실제 런타임 연결을 구분한다. MD에 생성됐다고 적혀 있어도 현재 게임에 자동 적용된 것으로 간주하지 않는다.
- 신규 이미지가 필요하면 위 문서의 참조 프레임, 방향, 바닥선, 표시 크기를 프롬프트와 검수 항목에 포함한다.

## UI 검토 기준

- [UI 전반 검토 v8](dad-ui-review-v8.md): 홈 구조, 글자 가독성, 공통 버튼·카드 개선 및 렌더링 검증 기준. 새 UI 에셋 제작 전에 확인한다.

- [병맛 UI 방향 v9](dad-ui-direction-v9.md): 기획 v4/v5와 현재 콘텐츠를 바탕으로 한 디자인 기준. [3면 정적 시안](dad-ui-concept-v9.html). 일반 복도 로비안보다 이 방향을 우선한다.

- [아빠 전쟁 시작 에셋 v11](dad-war-opening-assets-v11.md): 회사 스플래시 규격, 타이틀 1장, 시나리오 6컷. [동작 미리보기](dad-war-opening-preview-v11.html). 원본은 `Assets/DadSchoolBoss/Art/V11Opening/`.

- [장착 장비 외형 v14](dad-outfit-appearance-v14.md): 투명 장갑/신발 파츠, 기존 안경·모자·소품 재사용, 상의 색상 변형. 현재 장착 데이터와 메인 캐릭터에 연결됨. [비교 미리보기](dad-outfit-preview-v14.html).

- [V15 착용 안경 보정](dad-eyewear-fit-v15.md): 투명 렌즈 테두리, 자세별 눈 앵커, Unity 캡처.

- [V16 전체 장비 외형 검수](dad-equipment-visual-audit-v16.md): 현재 합성 방식 불합격, 슬롯별 결함·재제작 우선순위.

- [V17 장비 외형 수정](dad-equipment-appearance-v17.md): 상의 16포즈·착용 모자, 손발 교체 및 그립 수정, Unity 검수 화면.
