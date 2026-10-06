# 2026-10-06 · Phase 6 통합 예제와 실행 검증

- 목표/기준: track `de4bb691aed3124443c4bfe3cfd085e3275002a8`에서 `codex/game-scenes-p6-validation`을 생성해 승인된 마지막 예제·반복 Play·소비 프로젝트·원래 Editor Player·회귀를 검증한다.
- 상태: 자동 검증 완료, 사용자 확인 대기. main 병합과 브랜치 삭제는 보류한다. P6 commit/push와 track FF 통합의 정확한 SHA는 Git 이력·최종 보고에 남긴다.
- 배정: scene_runtime(6.1-sol/high)은 비동기 sample/fixture, track_guidelines(luna/medium)은 최소 소비 도구/인간 절차. 부모가 Unity·검증·문서·Git과 최종 리뷰를 소유했다. 허용 경로를 나누고 기존 두 agent를 재사용했다.
- 결정: Core 변경 없이 별도 sample UI/Input assembly·명시 asset 생성 메뉴·자동 stop 복구를 제공했다. 4×2 반복 Play에서 Bootstrap 잔류 결함이 재현되지 않아 reset을 넣지 않았다. fixture 가정 오류2회는 제품 Red가 아니다.
- 문제/해결: NEW meta GUID/NUnit 접수 오류는0건·compile 실패로 기록하고 실제 입력 차단 Red2/0/2→Green2/2를 확인했다. NewScene 뒤 파괴된 settings wrapper는 경로 재로드로 보완했다. background delayCall은 idle update1회 복구로 변경하고 실제 normal stop에서 원래 InitScene 복구를 확인했다. 투명 modal 겹침은 불투명 가림막으로 보완 후 실제 화면 재확인했다. 소비 첫 build는 성공했지만 driver의 빈 stdout 가정이 실패해 Player0으로 기록했고, actual log 검증 후 새 output에서 성공했다.
- 증거: [통합 기록](../validation/scene-integration/README.md). 전체 Edit240/240·Play201/201, 실패0·skip0; sample최종2/2. Reload8/8; 원래 Editor Windows Player 두 모드 각10관찰; 동일6000.3.18f1 소비 build/Player11관찰. compile 완료·예상 회귀 오류 보존·최종 Console0·원래 Editor ready. Player BuildReport.warning0과 compiler warning0은 구분한다.
- 보호: 사용자 원래 Unity 파일4와 build raw bytes를 보존했다. Native build의 URP cache·PlayerSettings default·UnityConnect 변경을 작업 전 기준으로 복원하고 Editor 재import/저장 후 hashes를 검증했다. sample GUID8과 소비 allowlist86 current/copy hashes를 확인했다.
- 미검증: [인간 실행 절차](../SCENE_TRANSITION_ACCEPTANCE.md)의 화면비·실제 키보드/포인터·시각 UX. 다른 Unity 버전·플랫폼·crash 복구·모든 autoStart/Addressables 콘텐츠를 검증한 것으로 확대하지 않는다.
- 다음: 사용자 Git diff·Unity 확인과 명시적 답변을 기다린다. 이후 최신 main/track·필수 gates를 재확인해 main 통합/push 및 도달 가능한 작업 브랜치 정리한다. 지금은 Unity 저장·Editor 유지 후 절전한다. 일회성 출력은 영속 증거 대조 후 명시 경로만 정리하며 공통 baseline은 후속 확인까지 보존한다.

Git/정리 결과: P6 소스·증거116파일을 `76939ba678d217f7e4787122a14139e73e4c3a51`로 커밋·Phase push했다. 정확한 CI는 미구성(main 무보호)이고 CI 성공이 아니다. 별도 기록 commit 뒤 같은 입력 hash를 검증해 track FF/push하며 main은 인간 확인까지 유지한다. 실제 종료된 실행 소유 임시 폴더12개만 제거하고 필요한 영속 결과/로그/hash는 보존했다. EOF 빈줄/Unity 빈 scalar 공백만 예외로 둔 scoped whitespace 검사와 verifier를 통과했다.
