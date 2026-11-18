# Android Unity 로고 이후 검은 화면 수정

사용자 보고: 갤럭시 S7 정도의 기기에서 Unity 로고 이후 검은 화면. 현재 기기는 없어 실행 로그·정확한 모델/Android 버전은 확보하지 못했다. 특정 기기의 원인 확정이나 해결 완료 판정은 아직 할 수 없다.

## 확인한 코드 결함

1. `DadAdMobProvider.GatherConsent()`가 `DadAdPrivacy.Busy=true`로 설정한다.
2. 기존 `DadRewardedAds.Busy`에는 동의 대기 상태도 포함됐다. 게임의 `Update`와 `OnGUI`는 Busy이면 즉시 반환했다. 따라서 동의 응답이 끝나기 전 타이틀을 전혀 그리지 않았다.
3. 동의 콜백은 `MobileAdsEventExecutor.ExecuteInUpdate`에 들어가지만, SDK11.3의 이 함수는 큐에 추가만 한다. 실행기를 만드는 `Initialize()`는 호출하지 않는다. 기존 코드는 동의 완료 뒤에야 `MobileAds.Initialize()`를 호출했고, 그 시점에서 실행기가 만들어지므로 순환 대기가 가능했다.

이는 기기 성능과 별개로 검은 화면을 설명할 수 있는 소스 결함이다. 사용자 기기에서 이 경로가 실제 발생했는지는 로그가 없어 미확인이다. 빌드/컴파일 성공 검사는 이 런타임 순서를 잡아내지 못했다.

Google SDK 소스: [큐 구현](https://github.com/googleads/googleads-mobile-unity/blob/v11.3.0/source/plugin/Assets/GoogleMobileAds/Common/MobileAdsEventExecutor.cs), [SDK 초기화](https://github.com/googleads/googleads-mobile-unity/blob/v11.3.0/source/plugin/Assets/GoogleMobileAds/Api/MobileAds.cs).

## 수정

- 타이틀 프레임이 나오도록 두 프레임 후 동의 처리를 시작한다. 그 전에는 `Update`가 먼저 동의 요청을 시작하지 않도록 대기 시각을 무한대로 둔다.
- 동의 요청 전에 콜백 실행기를 명시적으로 만든다. 이는 광고 SDK 초기화나 광고 요청을 동의보다 먼저 수행하는 것이 아니다.
- 동의/네트워크 대기를 게임 정지 조건에서 제외한다. 동의 확인 없이 광고를 요청하는 우회는 하지 않는다.
- 실제 보상형 광고가 표시 중이어도 게임의 기존 화면을 계속 그린다. 이때 게임 진행과 UI 입력은 멈추지만 화면 자체를 지우지 않는다.
- 최초 동의 시작에 예외가 나면 광고 준비를 보류하고 경고를 남긴다.
- 저장 형식/앱 ID와 GLES3 설정은 유지한다. 버전은0.3.1이며 테스트 APK는 디버그 키로만 서명한다.

## 검증과 한계

Android 스크립트/SDK 설정 검사와14개 보상·저장·동의 대기 검사, 콘텐츠40개가 통과했다. Windows 무창 플레이어14개 검사도 종료코드0으로 통과했다. SDK 실행기를 실제로 만들고 동의 콜백 큐가 광고 SDK 초기화 전에 처리되는지 확인했다. 이 검사는 Android 네이티브 동의 UI나 그래픽 드라이버를 검증하지 않는다.

수정 APK는 별도 Unity6 빌드 사본 `C:/Users/user/DadWarApkTest`에서 만든다. 원본 변경 전 세 소스 파일은 그 사본의 `Build/StartupFix/originals`에 보존했다. 다른 미커밋 Unity6 전환 변경은 합치지 않았다. Play 정식 출시 승인이나 S7 실기기 검수 완료를 뜻하지 않는다.

## 배포 산출물

- Unity6000.1.5f1, ARM64/GLES3, 최소API24, 타깃API36. 빌드 성공, 오류0/경고14, Android 의존성 해결True, 소요3분19초.
- 빌드 직후0.3.1(2) APK에 Unity 공식 Application Patcher1.3.3의 Android 보안 패치를 적용하고0.3.1(3)으로 재서명했다. 동일한 로컬 디버그 키를 사용했으며 기존0.3.0 APK와 인증서 SHA256이 일치한다.
- 보안 패치 후7개 산출물 검사(ARM64, 네이티브16KB, 데이터, 앱ID, API36, ZIP16KB, 서명) 통과. `com.manducorporation.dadwar`, versionName0.3.1/versionCode3을 최종 매니페스트에서 확인했다.
- APK SHA256: `ff720a7496573affcb6054971bfb0ec16ace10cfdd49112f0c78c369d9be9b04`.
- 배포 파일: `DadWar-v0.3.1-arm64-test.apk`. 기존 테스트 앱에 덮어 설치할 수 있는 서명이며, 먼저 삭제할 필요는 없다. 다른 서명으로 설치한 앱이면 업데이트가 거절될 수 있으므로 저장 데이터 삭제부터 권하지 않는다.
- 증거: `dad-startup-runtime-checks.txt`, `dad-admob-sdk-checks.txt`, `dad-ad-flow-checks.txt`, `dad-startup-android-build.txt`, `dad-startup-android-artifact.json`, `dad-startup-android-security.txt`.

초기 APK 빌드의 라이선스 프로토콜 충돌은 작업 전용 라이선스 채널로 해결했다. 실패 로그는 `Build/StartupFix`에 보존했다. 현재 Play 배포 키가 아닌 디버그 서명이며, 실제 Android/S7 실행은 여전히 미검증이다.
