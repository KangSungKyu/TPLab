# 2026-10-08 · UIContext P7 통합·소비·성능·최종 확인

## 작업과 기준

- 사용자 요청: 승인한 UI P0~P7을 진행하고 Virtual ScrollRect는1,000/10,000 규모의 기능·성능도 확인한다.
- 선행: [P6 회고](2026-10-08-10-ui-installation.md), track tip `4ccc7e3489eb6706d376b2605ca7b46f2972b630`, 작업 branch `codex/game-ui-p7-validation`. main `8ce768d96f79ed8328143bebc31e77f060f51205` 보존.
- 상태: 구현·원본 전체 회귀·base/optional consumer·성능·최종 Single/Additive sample 자동 검증 완료. 실제 장치·시각 수락 및 main 통합 대기.

## 결정과 변경

- 재사용 소비 도구/5개 C# harness를 `tools/ui-consumer`와 `tools/run_ui_source_consumer.py`로 승격했다. 정확한 SHA·allowlist·meta·라이선스·원본/사본 hash, fresh 결과/로그, 실제 PID/종료/실행 수와600프레임 marker 연속성으로 검증한다. 기존 Core 소비 도구의 pure helper를 사용하며 Core/Input/UI Runtime을 바꾸지 않았다.
- 숨긴 graphics Player는 D3D12로 보고되어도 실제 draw0이었다. 동일 바이너리를 표시하되 활성화를 요청하지 않는 창으로 실행한 controlled 비교에서600프레임 양수 draw를 확인했다. launcher만 보정했으며 Camera/UI product를 고치지 않았다.
- virtual 목록은 Count0에서도 viewport 예산 이내 inactive 셀을 보관한다. consumer의 잘못된 owned0 기대와 sample의 Content.childCount=active 기대를 보정했다. 표시/비표시·binding/index·예산·누적 생성 검사는 유지하고 실제 snapshot을 추가했다.
- 전체 회귀에서 이미 초기화된 Addressables의 locator 목록이 이전 Core fixture teardown으로 비어 UI fixture 초기화만 실패했다. UI 테스트는 필요할 때 자기 소유 빈 locator만 등록·정리하고 PlayerPrefs를 finally에서 복원한다. 기존 locator·Core native failure 계약과 runtime 초기화 로직은 보존했다.
- Single sample 종료는 마지막 일반 씬 해제 금지에 걸렸다. 기능/렌더 검사를 끝낸 뒤, consumer 검증 driver가 빈 parking 씬을 생성하고 Quit까지 보관하도록 했다. Manager에 등록하지 않으며 실제 Bootstrap/root 종료·Manager Stopped/소유 씬0·UI→Input 정리 관측은 유지했다.
- Python helper 실제 Red/Green과 실제 Git attributes CRLF/LF 사례를 확인했다. 원본 raw hash와 canonical Git blob hash를 구분했다. 실패 후 sample 실행 수가0으로 남던 기록과 완료된 performance가Unmeasured로 남던 시점도 보정했다. 원래 실패 JSON은 수정하지 않는다.

## 검증과 한계

- 원본 Editor PID24376의 source `99df25dd6b89fd7f3e322c8033b61eb1364f495e`: Edit318/318 + Play306/306 =624/624, 실패0·skip0. UI86, 기존Core/Input538. Play CLI transport1은 동일 완료 run 파일에서306개를 복구했으며 재실행이 아니다.
- 최종 소비 source `8310a94bcb674f28bee2c246c4e5eae9d6c83804`: 원본624개의 Assets/Packages/ProjectVersion 입력은 그대로이며 변경된 captured 입력은 consumer-only sample harness1개다. 각 결과는 실제 source SHA를 유지한다.
- base source consumer의 실제 Editor build1 + graphics Player3(smoke/scroll/Canvas) 통과. optional Input 일반3단계도 통과했으나 최초 sample fixture 실패로 그 전체 invocation은 실패였다. 보정한 Additive sample 별도Player는77checks/30양수draw/목록7snapshots/수동Continue4/UIcleanup2를 통과했다. 최종source8310a94의 Single/Additive combined consumer는 각각Editor1/build2artifacts/Player4를 통과했다. 각 sample은30양수draw(min11), inventory7/Continue4/cleanup2; Single80/Additive78 checks. Manager Stopped/registered0/owned0과 UI→Input 종료도 통과했다. 결과는 [P7 증거](../validation/ui-system/p7/README.md)에서 확인한다.
- native1000↔virtual1000 동수 비교 및 virtual10000:120 warm+600측정 프레임. virtual두 arm은41owned/41created/0destroyed; median 처리 시간은 낮았지만 GC alloc은 높았다. Canvas1/3/4는 같은128HUD+3popups, 같은5회schedule. 측정은 한 장치의 탐색 결과이며 인과/전체플랫폼/성능보장은 아니다. [성능 보고서](../UI_PERFORMANCE.md)와 rawJSON은 outlier/N/A를 보존한다.
- compile error0·제품 Console 오류0, 예상 negative-test 로그는 별도 보존. native compiler registry 영구 복구는 주장하지 않는다. marker가 있는데 draw0인 실행, setup 실패, transport/ImportError0건은 통과로 묶지 않는다.
- Windows64 Mono/Unity6000.3.18f1/D3D12/1280x720 범위다. 물리 장치·화면비·복원·최종UX는 [수락 문서](../UI_ACCEPTANCE.md)를 따른다. IL2CPP/다른 플랫폼·Unity버전/장기 soak와 독립 overdraw/batch 원인 capture는 미실행/N/A다.

## 다음 작업과 통합

- 최종 자동 검증·문서·회고 후 현재 tip을 ancestry/CAS로 UI track에 통합한다. main과 branch 삭제는 실제 입력/시각 수락이 끝날 때까지 보류한다. P7 source commit들은 유지하며 squash/rebase하지 않는다.
- 새 UI는 `Assets/TPLab/UI` 개발 source다. 0.0.1 upm/tag/Release·기존 Core/Input 구조를 변경하지 않았다. UI package 배포는 별도 후속 작업이다.
- 수행한 임시 파일의 증거는 `doc/validation/ui-system/p7`에 보존한다. 최종 두 consumer project/Player는 사용자 확인용으로 유지하고, 확인·필요 증거 보존 후 소유한 경로만 제거한다.
- 향후 GC 감소는 제안이며 이번 범위에서 성급하게 pool/CTS/전체 binder를 바꾸지 않는다. 현 소유 상한·취소/세대·late completion 계약과 같은 조건의 측정으로 별도 검토한다.
- 이번 UI 요청에 Unity/PC 종료는 포함되지 않았다.

P7 마감 보완: 증거 JSON/log/text는 폴더 범위 `.gitattributes -text`로 실행 당시 raw bytes를 Git에 보존하고 index blob과 파일 bytes를 대조했다. 기존 source/과거 증거의 attributes는 바꾸지 않았다. 문서18개/상대링크599개/ownedasset58개 GUID·입력476·보호6 정적검증을 통과했다. 소유 임시 경로18개를 제거하고 최종 two-entry 확인용consumer2개만 보관했다.
