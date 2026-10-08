# UIContext P2 검증

기준 codex/game-ui-p2-loading / 69b0826. 원본 Unity6000.3.18f1 Editor PID24376, Connector0.4.1에서 테스트를 실행한다. 테스트 준비 시 ready·compile/update/play false·InitScene dirty false·Console 오류0을 확인했다.

## 계약

- 직접 prefab과 명시 key provider를 구분하고 Prepare/Open은 Context의 같은 key 로딩을 공유한다. provider/root/source는 borrowed다.
- Prepare caller 취소는 그 대기만 종료한다. 공유 준비는 owner token을 사용하고 실패한 항목은 다음 명시 요청에서 재시도한다. 자동 반복 재시도는 없다.
- owner 종료 후 늦은 provider 결과는 표시/캐시에 반영하지 않고 borrowed source/provider를 파괴·해제하지 않는다.
- Reuse는 정의별 inactive clone 최대1개다. 표시마다 새 handle/token/cleanup을 생성하고 이전 handle 종료가 새 표시를 변경하지 않는다.
- 실패/취소한 Opening 및 cleanup/Closed observer 실패의 clone은 보관하지 않는다. 종료 observer가 시작한 새 표시는 이전 clone 처리와 독립이다.

## 실행 상태

실제 Red: [Edit](red-edit.json) total5/passed0/failed5/skip0, [Play](red-play.json) total7/passed0/failed7/skip0. 원본 Editor PID24376, CLI5288/1784. 모든 실패가 P1의 Asset providers and reuse require P2 NotSupportedException으로 발생했고 컴파일 오류0이다. [Red 입력](red-inputs/test-inputs.json)368개·[보호 파일](red-inputs/preserved-inputs.json)6개 unchanged를 보존했다.

## Green과 회귀

SourceRevision: `cea4268b82e0f119e0d4dbc8c788b7632d21f3ee`. 새 public signature 없이 Runtime2파일과 새 테스트2파일/meta2개를 구현했다.

| 실제 실행 | total | passed | failed | skipped | CLI PID |
|---|---:|---:|---:|---:|---:|
| [UI EditMode 전체](final-edit.json) | 14 | 14 | 0 | 0 | 17548 |
| [UI PlayMode 전체](final-play.json) | 16 | 16 | 0 | 0 | 28744 |

동일 원본 Editor PID24376에서 새 P2 Edit5/Play7과 기존 P1 Edit9/Play9를 포함한30개가 통과했다. 컴파일 오류0, 테스트 이후 제품 Console 오류0. [정확한 입력](test-inputs.json)368개·보호6파일 unchanged다. Refactor 리뷰에서 public asset Prepare와 inactive clone의 data hook을 구분했고 추가 구조/동작 변경은 필요하지 않았다.

## 연결 장애 기록

최초 Green 시도 CLI20724는 cannot connect to Unity 시간 초과로 nonzero 결과가 없었다. 이를 실패 테스트/통과/Red로 집계하지 않고 [별도 기록](transport-rejected.json)했다. 직접 health, 같은 project/PID, native TestRunner IsRunActive=false, compile/play=false 및 main-thread 읽기 응답으로 연결 복구를 확인했다. Editor 재시작·source 변경 없이 새 invocation에서 위30개를 실행했고 CLI20724의 결과로 대체하지 않았다.

## 검증 한계

소비 프로젝트/Player/Profiler/최종 UX는 P7 미실행이다. 기존 P1 root10 검증은 [P1 기록](../p1/README.md)의 당시 근거이며 이번 P2의 새 실행으로 합산하지 않는다. HUD·parent graph·Canvas host/order/render-only·modal/input·Virtual ScrollRect는 후속 Phase다. main/upm/version/tag/Release는 변경하지 않았다.
