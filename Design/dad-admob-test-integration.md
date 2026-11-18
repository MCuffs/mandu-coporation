# AdMob 보상형 테스트 광고 연결

작성: 2026-11-10. 적용 브랜치: `codex/dad-war-admob-test`.

## 구현

- Google Mobile Ads Unity 11.3.0과 External Dependency Manager 1.2.187을 OpenUPM에서 설치하고 lock 파일로 고정했다. 11.3의 패키지 최소 Unity는2019.4이며,11.4 이후는2022.3이다. 현재2021 프로젝트의 호환성을 유지하기 위한 선택이다. Android 정식 빌드는 별도 엔진 업그레이드 검증이 필요하다.
- Android 표준 SDK25.4.0, UMP4.0.0 의존성을 사용한다. 최소 Android API24로 올렸다. 패키지 설치는 Gradle 의존성 해결·APK/AAB 빌드 성공과 다르다.
- Google Android 데모 앱 ID `ca-app-pub-3940256099942544~3347511713`, 보상형 데모 단위 `ca-app-pub-3940256099942544/5224354917`만 사용한다. 현재 소스에서 상용 ID로 전환하는 자동 경로는 없다.
- Android에서만 SDK를 초기화한다. 기존 Windows v0.3 다운로드에는 이 변경이 들어 있지 않다. PC에서 실제 Google 모바일 광고가 표시되는 것으로 안내하지 않는다.
- 매 실행 UMP 동의 정보 업데이트 → 필요한 동의 폼 → `CanRequestAds()` 확인 → SDK 초기화 → 보상형 미리 로드 순서다. 동의 처리 실패 시 광고 없이 게임을 계속할 수 있다. 개인정보 선택 변경이 필요한 경우 설정에 버튼을 제공한다.
- 보상형은 본편 첫3판 이후 승리 결과의 선택 버튼에 연결한다. 완료 보상 이벤트를 받은 뒤 광고가 닫히면 코인 보상을 확정한다. 단순 닫기·표시 실패·준비 안 됨에는 지급하지 않는다. 기존 영수증·중복 방지·30초 대기·세션5회 상한을 유지한다.
- SDK 콜백은 메인 스레드 실행 큐로 보낸다. 광고 로드 실패 재시도는5→10→20→40→60초 상한이며, 캐시는55분에 폐기한다. 개인정보 선택 변경 전의 캐시와 진행 중인 이전 로드 결과도 폐기한다.
- 공개/게임 내 개인정보 안내를 함께 갱신했다. SDK 정보 수집이 없다고 신고하면 안 된다.

## 실제 검증 범위

`dad-admob-sdk-checks.txt`는 실제 패키지의 API 로드·설정·Android 스크립트 컴파일 검사다. `dad-ad-flow-checks.txt`는 가짜 provider를 이용한13개 보상/취소/저장 검사다. 둘 다 실기기 광고 노출 증거가 아니다.

현재 Android APK/AAB 생성·Gradle 해결·휴대폰 광고 노출·동의 폼·보상/취소 실기기 검증은 미완료다. AdMob 계정도 아직 없으므로 수익이 발생하지 않는다. 테스트 ID는 정식 출시 전 실제 앱/광고 단위로 교체하고 개발 기기를 테스트 기기로 지정해야 한다. AdMob에서 지역별 개인정보 메시지 설정도 완료해야 한다. 서버 보상 검증은 아직 없다.

실기기 확인: 본편4판 이후 승리 → 광고 준비 후 선택 → ‘Test Ad’ 확인 → 완료 시 추가 코인1회 → 다시 진입/재실행 시 중복 없음 → 중도 닫기·로드 실패는 보상 없음 → 동의 변경과 앱 복귀 → 네트워크 차단 상태에서 플레이 가능.

## 가장 빠른 Play 배포 경로

계정 등록과 Android 빌드 준비를 병행한다. 내부 테스트는12명·14일을 기다리지 않고 시작할 수 있지만 개발자 계정과 필수 인증, 업로드 가능한 AAB는 필요하다. 내부 테스트 참여 기간은 신규 개인 계정의 필수 비공개 테스트14일을 대신하지 않는다.

현재 개인사업자·Console 미가입·D-U-N-S/웹사이트 없음 상태에서 즉시 정식 공개를 약속할 수 없다. 사업용 조직 계정은 실제 사업자 정보로 D-U-N-S·공식 사이트/이메일·Google 인증을 준비한다. 신규 개인 계정을 사용한다면12명 연속14일 비공개 테스트와 프로덕션 액세스 승인을 거쳐야 한다. 조직 계정 준비가 개인 계정 테스트보다 빠르다고 단정하지 않는다.

## 공식 근거

- [SDK 설치](https://developers.google.com/admob/unity/quick-start)
- [테스트 ID](https://developers.google.com/admob/unity/test-ads) — Unity rewarded 안내 일부 예제의 ID와 달리 포맷별 공식 표의 보상형5224354917을 사용했다.
- [보상형 생명주기](https://developers.google.com/admob/unity/rewarded)
- [UMP](https://developers.google.com/admob/unity/privacy)
- [데이터 공개](https://developers.google.com/admob/android/privacy/play-data-disclosure)
- [신규 개인 계정 테스트](https://support.google.com/googleplay/android-developer/answer/14151465?hl=ko)
- [내부 테스트](https://support.google.com/googleplay/android-developer/answer/9845334?hl=ko)
