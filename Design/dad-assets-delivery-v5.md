# 에셋 납품 v5
2026-11-04. 요청서: dad-asset-request-v5.md.

저장 폴더: /Users/jeongminsu/Desktop/dad-assets

## 납품 수량

| 폴더 | 최종 파일 |
| --- | --- |
| ui | 256×256 투명 PNG 21개 |
| gear | 256×256 투명 PNG 18개 |
| chars | 768×768 투명 캐릭터 프레임 63개 + 소품 2개 |
| bg | 1080×1920 불투명 배경 3개 |
| story | 1080×1350 불투명 만화 3개 |
| app | 1024×1024 불투명 앱 아이콘 1개 |
| audio | 44.1kHz / 16bit / mono WAV 21개 |

최종 PNG 111개 + WAV 21개. _sources의 생성 원본과 preview_*.png는 이 수량에 포함하지 않는다.

## 바로 볼 파일

- preview_icons.png: UI·장비 전체
- preview_characters.png: 캐릭터와 소품 전체
- asset_manifest.json: 최종 파일 경로, 크기, 피벗, 원본 시트·셀 정보
- qa.json: 픽셀 크기·알파·아이콘 여백 검사와 검수 주의사항
- generation_jobs.json: 내장 image_gen에 전달한 프롬프트·참고 이미지·수정 프롬프트·최종 원본 경로
- audio/LICENSES.txt: 절차 합성 음원 출처와 CC0 안내

## 제작·정규화

그림은 Codex 내장 image_gen으로 제작했다. 기존 Hero.png와 Enemies.png를 인물·화풍 참고로 사용했다. 새 껌 캐릭터는 벗겨진 이마, 수염·다크서클·배를 넣어 다시 디자인했다. 두 새 보스와 피구 캐릭터도 성인 나이 단서를 넣었다.

아이콘은 생성 시트에서 개별 추출하고 256px 캔버스의 16px 여백을 맞췄다. 내장 도구의 수정 결과에도 남은 반투명 외곽 광은 내보내기 알파 매트로 제외했다. 수정하지 않은 생성 원본은 _sources에 남겼다. 배경은 뒷벽/바닥 경계를 요청 가이드의 y=480에 맞춰 구간별 리사이즈했다.

캐릭터는 장면 그림이 아니라 개별 투명 프레임으로 분리했다. 기본 발 피벗은 (384,720), Unity normalized pivot은 (0.5,0.0625)이다. 날아감은 중앙 배치다. 동일 시트에는 공통 축척을 사용하며, 걷기가 반복된 의자·급식 보스·체육 보스·피구 캐릭터는 별도 지나감 포즈를 추가했다. 급식 보스 돌진은 잘림이 생기지 않도록 별도로 제작했다.

## Unity 연결 시 주의

1. PNG 텍스트·등급 테두리·그림자·타격 효과는 넣지 않았다. 모두 Unity가 그린다.
2. 캔버스 크기·알파·아이콘 여백 검사는 통과했다. 사람의 동작 일관성과 애니메이션 재생 품질까지 자동 검증한 것은 아니다.
3. 의자와 큰 보스의 넓은 동작은 소품이 캔버스를 벗어나지 않도록 맞췄다. 특히 boss2_dash_0은 웅크린 넓은 자세라 높이가 작다. Unity에서 루프를 재생하며 얼굴 크기·발 접지·광학적 축척을 보정해야 한다. 원본 시트와 단독 포즈가 함께 있어 재분할할 수 있다.
4. 아빠 특수 포즈에는 기존 복장에 없던 넥타이를 추가하지 않고 조끼 자락을 사용했다. 만화 속 아들은 성인으로 표현했다.
5. 소리는 기존 곡이나 녹음을 가져오지 않은 직접 합성본이다. 보스 포효는 사람 음성 녹음이 아닌 합성 그로울이며, BGM은 60초짜리 임시 합성 루프다. 출시 전 게임에서 소리 크기·반복 연결·음색을 청취 검수한다.
6. 이번 작업은 에셋 폴더 납품이다. 진행 중인 Unity 코드·씬에는 연결하거나 덮어쓰지 않았다.

## 재생성 도구

- Tools/package_dad_assets_v5.py: 생성 원본 → 파일 분리·규격·피벗·검사·미리보기
- Tools/dad_assets_v5_audio.py: 효과음·징글·60초 BGM 직접 합성

패키징 재실행:
python3 Tools/package_dad_assets_v5.py /Users/jeongminsu/Desktop/dad-assets/generation_jobs.json /Users/jeongminsu/Desktop/dad-assets

음원 재실행:
python3 Tools/dad_assets_v5_audio.py /Users/jeongminsu/Desktop/dad-assets/audio

