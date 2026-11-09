# R-03 Android 빌드 전환 — 준비 및 설치 대기

2026-11-10. **현재 상태: Windows 관리자 설치 대기. Unity6 업그레이드·APK/AAB 빌드 성공을 의미하지 않는다.**

기존2021 프로젝트(`C:/Users/user/DadWarLatest`, `codex/dad-war-release-fixes`)를 보존하고, 별도 작업 사본 `C:/Users/user/DadWarAndroid6` / `codex/dad-war-android6`를 준비했다. 저장·앱 ID·처리방침·긴 화면 수정도 포함한다. 이 브랜치를 검증 전에 출시 기준으로 합치지 않는다.

## 준비 완료

- Unity 공식 릴리스 API와 릴리스 페이지에서 Unity6.3 LTS `6000.3.28f1`, revision `8f082d2fe476`을 확인했다. [공식 릴리스](https://unity.com/releases/editor/whats-new/6000.3.28f1)
- Editor와 Android Support 설치 파일을 모두 내려받아 공식 메타데이터의 크기·MD5를 확인했다. Editor의 Windows Authenticode 서명도 Valid / Unity Technologies SF로 확인했다.
- 설치 파일은 `Build/AndroidUpgrade/installers/`에 있으며 Git에 올리지 않았다. 조립용 중복 조각은 검증 뒤 정리했다.
- API36 플랫폼, Build Tools36, Command Line Tools16, Platform Tools37, CMake3.22.1, OpenJDK17.0.18 패키지를 준비했다. OpenJDK의 실제 `java -version`을 확인했다. NDK r27c는 같은 버전의 기존 설치를 복사해 준비한다.
- SDK 준비 영역은 `Build/AndroidUpgrade/toolchain-stage/Editor/Data/PlaybackEngines/AndroidPlayer`다. 실제 Editor 설치 폴더에 아직 연결한 상태가 아니다.
- `DadAndroidBuild`에 Android 전용 ID·ARM64·IL2CPP·Target API36·Minimum API23·OpenGLES3 설정과 테스트 APK/AAB 빌드 진입점을 추가했다.
- `check_dad_android_artifact.py`에 최종 manifest ID/API, ARM64 payload, ELF16KB 정렬, APK zipalign/서명 및 AAB bundletool 검사를 마련했다. 실제 산출물에는 아직 실행하지 않았다.
- 전체 런타임·Editor 소스는 기존 Unity2021 Mono 컴파일러에서 사전 컴파일 통과(기존 미사용 필드 경고3개). `UNITY_6000_0_OR_NEWER` 전용 코드와 Unity6 실행 검증을 대신하지 않는다.

## 자동 설치가 멈춘 이유와 필요한 사용자 작업

Windows가 설치 실행을 **‘요청한 작업을 수행하려면 권한 상승이 필요합니다’**로 거절했다. 일반 압축 해제 도구로도 이 NSISBI 설치 파일을 풀 수 없었다. 권한 상승이나 실행 파일 변조를 시도하지 않았다. Unity Hub의 기본 설치 경로는 원래 `C:/Program Files/Unity/Hub/Editor`로 복원했다.

사용자가 아래 설치 파일을 순서대로 직접 실행하고 관리자 권한 창을 처리해야 한다.

1. `C:/Users/user/DadWarAndroid6/Build/AndroidUpgrade/installers/UnitySetup64-6000.3.28f1.exe`
2. `C:/Users/user/DadWarAndroid6/Build/AndroidUpgrade/installers/UnitySetup-Android-Support-for-Editor-6000.3.28f1.exe`

설치 대상은 `C:/Users/user/UnityEditors/6000.3.28f1`로 맞춘다. 두 번째 설치는 같은 Editor를 대상으로 한다. 설치 마지막에 Editor 자동 실행을 선택할 필요는 없다. 설치 경로를 다르게 선택하면 다음 검수 명령도 실제 경로에 맞춘다.

## 설치 뒤 수행할 작업

1. Editor 실행 파일·버전과 AndroidPlayer 폴더를 확인하고 준비한 SDK/NDK/JDK를 연결한다. 경로는 설치 사본 내부를 사용하며 기존2021 사본을 덮어쓰지 않는다.
2. 창 없는 배치 모드로 `DadAndroidBuild.ConfigureOnly`를 실행해 설정을 실제 적용한다. ProjectVersion 텍스트 변경만으로 완료 처리하지 않는다.
3. Unity6에서 네이티브 저장·에셋·경제·장비 검사를 다시 실행하고 API 변경/직렬화 차이를 수정한다. 업그레이드가 importer나 ProjectSettings에 만든 변경을 검토한다.
4. `-buildTarget Android -executeMethod DadAndroidBuild.TestApk`로 테스트 APK를 만든다. 같은 방식의 `TestBundle`로 AAB를 만든다. 두 진입점은 빌드가 끝나도 게임 창을 열지 않는다.
5. 실제 산출물에 `python Tools/check_dad_android_artifact.py <파일> --android-player <Editor/Data/PlaybackEngines/AndroidPlayer> --report <보고서>` 실행.
6. 실제 휴대폰의 설치·입력·앱 전환·저장·성능을 검수한다.

**테스트 서명 주의:** 빌드 도구는 로컬 debug key를 사용한다고 명시한다. 업로드 키를 생성·설정하거나 스토어 업로드 승인을 받은 상태가 아니다. 실제 출시용 서명과 Play Console 검수는 별도 관문이다.

준비 상태의 기계 판독 기록은 `dad-android6-preparation.json`. R-03은 설치·실제 빌드·산출물 검사·기기 검수가 끝나기 전까지 미완료다.
