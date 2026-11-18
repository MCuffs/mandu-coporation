# R-02 Android 식별자 수정

2026-11-09. Android 전용 applicationIdentifier를 `com.manducorporation.dadwar`로 명시했다. 이전 실제 조회값 `com.SecondFistStudio.`의 빈 마지막 구간을 제거했다.

Standalone ID·회사명·제품명은 바꾸지 않았다. 특히 데스크톱의 기존 persistentDataPath가 회사명 변경으로 달라지는 일을 피했다. Android 설정은 신규 출시 후보용이며, 서명된 최종 산출물의 ID 검증은 R-03 빌드 관문에 남는다.

Unity2021.3.2f1 배치에서 실제 `PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)`가 새 ID를 반환하는 것을 확인했다. 같은 실행의 네이티브 감사8개 그룹 모두 통과했다. 증거: `dad-android-id-checks.txt`. 아직 실제 Android 설치·업데이트·서명 검증은 완료하지 않았다.
