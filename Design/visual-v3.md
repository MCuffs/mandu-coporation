# 시각·모션 개선 v3

## 모션
- 주인공 걷기 8프레임, 공격 준비/타격/회수 8프레임.
- 적 걷기 프레임 및 가속/감속, 몸 기울기, 호흡, 발 접지 보정.
- 그림이 변형된 적 프레임 12와 14는 재생에서 제외.
- 공격 준비 0.10초 후 실제 피해가 발생하도록 전투와 애니메이션 동기화.
- 넉백 상승/반동, 제압 후 쓰러짐/페이드, 타격 파편, 점진적인 이펙트 감쇠.

## UI
- 제목: Black Han Sans. 본문/강조: Gothic A1 Regular/Bold. 프로젝트에 포함.
- 초상/레벨/구역/시간/체력 HUD 및 피해 잔상 게이지.
- 전용 기술 아이콘, 기술 카드 등장/선택 반응, 버튼 눌림 상태.
- 원형 필살기 충전 게이지와 준비 상태 점멸.
- 시작/결과/지도 화면의 글자 크기와 색/테두리/간격 통일.
- 일시정지와 효과음 토글. Escape로 일시정지.

## 아트 원본
내장 image_gen 도구 사용. PNG는 Assets/Alley/Resources 안에 저장했습니다.
- HeroMotion.png
- EnemyMotion.png
- AbilityIcons.png
생성 프롬프트: Design/visual-v3-prompts.json.
발 피벗 메타데이터: HeroMotionPivots.json / EnemyMotionPivots.json.

## 글꼴 출처
https://github.com/google/fonts/tree/main/ofl/gothica1
https://github.com/google/fonts/tree/main/ofl/blackhansans
라이선스 원문은 Design/Licenses 및 Windows 빌드 Licenses 폴더에 포함합니다.

## 한계
2D 프레임 기반 시제품이며 방향별 전체 애니메이션이나 수작업으로 다듬은 최종 원화는 아닙니다.
모바일 실기기 검증과 화면 비율별 조정은 다음 단계입니다.
