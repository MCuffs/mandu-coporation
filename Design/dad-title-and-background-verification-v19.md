# V19 제목 잘림 재검증과 화면을 띄우지 않는 검수

2026-11-09. 사용자 요청: 팀원과 화면을 공유하므로 개발·완료 시 Unity 창을 띄우지 않는다.

## 첨부 화면의 원인

고해상도 글자는 폰트를 크게 렌더링한 후 GUI 행렬의 배율을 역으로 적용한다. 기존 오프닝의 `GUI.BeginGroup(540×960)`은 확대된 글자 사각형을 먼저 잘라 제목 오른쪽과 아래쪽 버튼 문구가 가려졌다. 배경 그림이나 폰트 파일 누락이 원인은 아니다.

현재 `DadOpening.DrawOpening`에는 그 GUI 그룹을 제거한 V18 수정이 들어 있다. 컷신 확대는 이미지 UV에만 적용하고 글자와 버튼은 정상 화면 좌표로 그린다. 이번 재개 작업에서는 수정 코드를 확인하고, 이전 Unity 검수 때 저장한 정상 캡처와 사용자 첨부를 비교했다. **새 Unity 렌더링을 실행한 검증은 아니다.**

## 화면 파일에 대한 오프라인 OCR 결과

| 검사 문구 | 사용자 첨부 | 수정된 기존 검수 캡처 |
|---|---|---|
| 아빠 전쟁 | 확인 실패 | 확인 성공 |
| 게임 시작 | 확인 실패 | 확인 성공 |
| 이야기 다시 보기 | 확인 실패 | 확인 성공 |

- [정상 화면](dad-commercial-ui-v18/00-title.png)
- [첨부 OCR](dad-title-clipping-ocr-v19-original.json)
- [정상 캡처 OCR](dad-title-clipping-ocr-v19-fixed.json)
- OCR은 macOS Vision을 사용하며 이미지 파일만 읽는다. 앱을 실행하거나 화면을 활성화하지 않는다. 이 검사는 저장된 복귀 유저용 타이틀 캡처의 글자 가독성에 대한 보조 검사이며, 현재 Unity 창 상태·터치·Android 렌더링 검사가 아니다.

재현:

```sh
swift Tools/verify_dad_title.swift Design/dad-commercial-ui-v18/00-title.png
```

## 이번에 변경한 검수 동작

- `DadPreviewBuild`가 빌드 완료 후 자동으로 Play Mode에 진입하던 코드를 제거했다.
- 휴대폰 해상도 검수 도구는 이미 존재하는 Game view만 사용하고, 없으면 새 창을 열지 않고 중단한다. `EditorWindow.GetWindow`를 호출하지 않는다.
- 프로젝트 `AGENTS.md`에 Unity/플레이어 창을 실행·전면 활성화하지 않는 요청을 기록했다.
- `Tools/run_dad_headless_checks.py`를 추가했다. Unity에 포함된 Mono 컴파일러/런타임만 사용하므로 Unity Editor나 플레이어 프로세스를 시작하지 않는다.

## 이번 실행 결과

- 런타임 및 에디터 코드 컴파일 성공. 기존 Alley 미사용 필드 경고 3개.
- [창 없이 실행한 시뮬레이션 검사 56개 통과](dad-headless-checks-v19.txt).
- 정상 캡처의 필수 문구 3개 확인. 사용자 첨부에는 해당 문구가 없어 의도대로 실패 판정.
- `git diff --check` 통과.
- 이번 작업 중 Unity Editor, 플레이어, Game view 창을 실행하거나 전면으로 가져오지 않았다.

시뮬레이션 재현:

```sh
python3 Tools/run_dad_headless_checks.py
```

이 명령은 순수 관리 코드의 조준·전투·난이도·재대결 규칙을 검사한다. 네이티브 저장 직렬화, 실제 GUI, 입력 제스처, Android 성능은 검사하지 않는다. 기존 V18의 Unity/플레이어 검사 결과와 이번 결과를 구분한다.

상업용 품질의 다음 차단 항목은 [V18 기록](dad-commercial-quality-v18.md)의 Android 엔진·모듈·ARM64·Target SDK 검증 및 실기기 플레이테스트다.
