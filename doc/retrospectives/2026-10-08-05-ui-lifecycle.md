# UIContext P1 수명 구현

2026-10-08. 기준 codex/game-ui-p1-lifecycle / cb82fb427e443f55f72568488bad7869a7510dbe. 목표는 root 소유 표시 세대·token·구독 정리·공유 Opened/Closed·실패/취소·Unity 파괴 fallback이었다.

## 변경과 결정

[실제 source545c342](../../Assets/TPLab/UI/Runtime/UIContext.cs)에서 UIContext/UIHandle/불변 정의/요청/hook·enum과 별도 Edit/Play assembly를 추가했다. UniTask GameObject destroy token의 AwakeMonitor를 재사용해 처음부터 비활성인 root도 정리한다. clone은 inactive storage 아래 생성·prepare하고 허가 이후 활성화한다. native Destroy 완료까지 닫기 결과를 기다린다. root/prefab/provider/input은 빌리며 이들의 수명을 종료하지 않는다.

BeginOpen은 즉시 handle을 발급하고 취소/실패 cleanup을 동일 경로에 둔다. 표시 token과 정상 닫기 연출의 owner token을 구분하고 caller 대기 취소가 cleanup을 중단하지 않게 했다. 오류가 있어도 역순 한 번 정리를 계속하며 main-thread callback 완료/실패 이후에도 Unity thread로 복귀한다. provider/reuse/HUD/host/modal 등 후속 정책은 NotSupported다.

## 문제와 배운 점

- Task.IsCanceled는 설치 UniTask AsTask의 예외 변환과 맞지 않았다. await의 OperationCanceledException과 UniTask 상태로 계약을 관찰하도록 테스트를 고쳤다.
- 추가 리뷰의 실제 실패2개로 Closed observer가 너무 이르게 호출되던 문제와 독립 popup 조합까지 차단하던 guard를 보정했다. 종료 완료 단계와 자기 handle/owner/cleanup/native의 재진입 범위를 각각 명시한다.
- after-await 자기 lifecycle 순환 대기 자동 검출은 구현하지 않는다. 동기 guard를 async 대기 전체에 걸어 외부 정상 Close를 막지 않는다.
- CLI 복수 이름 필터 total0은 통과/Red가 아니었다. 지원 class 필터로 nonzero 결과를 새로 확인했다.

## 검증과 남은 단계

[증거](../validation/ui-system/p1/README.md): 원본 PID24376 / Unity6000.3.18f1 / Connector0.4.1. 최초 Red Edit9/Play6 모두 실패·skip0. 최종 Refactor Edit9/9+Play9/9 및 기존 SceneRoot10/10 통과·실패0·skip0. 컴파일 오류0, root 회귀의 기대 Console 예외2/미예상0, 보호 파일6개 unchanged. 소비/Player/Profiler/사용자 UX 미실행.

소스30파일을545c342로 기록했다. 문서/증거/회고를 같은 단위의 별도 commit으로 보존하고 track에 fast-forward 통합한다. 정확한 문서/통합 SHA는 Git 이력에서 확인한다. main8ce768d·upm/version/tag/Release는 변경하지 않는다. P2는 shared keyed provider 준비와 inactive clone 최대1/definition 재사용의 실제 Red부터 진행한다. 사용자 최종 시각/입력 확인 전 main 병합/branch 삭제를 보류한다.
