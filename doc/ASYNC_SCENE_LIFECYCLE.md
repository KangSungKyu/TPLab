# 비동기 씬 준비·해제와 가림막 콜백

SceneOwnedRoot와 SingletonSceneRoot 모두 같은 비동기 계약을 사용한다. 주입된 서비스의 준비가 끝나야 씬 진행 콜백을 실행하며, UI 연출은 소비 프로젝트가 제공한다. ResourceManager와 GameSceneManager 자체는 이번 단계에 구현하지 않았다.

## 준비와 실행 순서

`가림막 표시 완료 → installer별 PrepareAsync 완료 → proceedAsync 완료 → 가림막 해제 완료`

- 기존 `Install`은 서비스 생성과 참조 주입을 동기로 수행한다. `ISceneRoot.IsReady`는 이 기존 계약을 유지한다.
- 새 `ISceneRoot.IsPrepared`는 모든 installer의 비동기 준비 성공 후에만 true다. Singleton의 Instance 존재나 MonoSingleton.IsInitialized만으로 게임을 시작하지 않는다.
- `SceneRootInstaller.PrepareAsync(root, token)`에서 manager 초기화·필수 자산·데이터 준비를 await한다. 기본 구현은 즉시 완료되므로 기존 installer도 사용할 수 있다.
- 준비는 Inspector에 지정한 순서대로 수행해 선행 시스템 의존성을 보존한다. 등록한 모든 준비가 끝나기 전에는 proceedAsync가 실행되지 않는다.
- PrepareAsync 호출들은 한 준비 시도를 공유한다. 호출자 토큰은 그 호출의 대기만 취소하며, 다른 대기자나 공유 준비를 취소하지 않는다. 실패한 준비는 캐시되며 재구성하려면 새 root를 사용한다.
- 게임 시작·입력 허용·씬 전환은 proceedAsync에서 명시적으로 수행한다. 이 콜백은 목적지 root 준비, UI 배치와 최종 표시 준비까지 기다린 뒤 완료해야 한다. 코어는 임의의 Awake/Start/Update 실행을 정지시키지 않는다.

```csharp
// root: 이미 활성화하여 설치한 ISceneRoot.
// 아래 콜백 메서드는 소비 프로젝트에서 구현한다.
_flow = new SceneRootFlow(root, ShowCoverAsync, HideCoverAsync, HandleFailure);
await _flow.PrepareAndProceedAsync(EnterSceneAsync, cancellationToken);

// 종료·전환 때 같은 flow를 재사용한다.
await _flow.ReleaseAndProceedAsync(EnterNextSceneAsync, cancellationToken);
```

ShowCoverAsync, HideCoverAsync, EnterSceneAsync는 각각 `Func<CancellationToken, UniTask>`이며, HandleFailure는 `Action<Exception>`이다. UnityEvent나 UI package는 추가하지 않았다. async void callback을 전달하지 않는다. 공유 완료는 여러 await를 지원하는 UniTaskCompletionSource를 사용한다. [UniTask 2.5.11 공식 계약](https://github.com/Cysharp/UniTask/tree/2.5.11)

## 해제 순서와 수명

`가림막 표시 완료 → 준비 중 작업 종료 대기 → installer별 역순 ReleaseAsync → 역순 Uninstall → 다음 화면 준비 완료 → 가림막 해제 완료`

- root.ShutdownAsync는 준비 수명 토큰을 취소하고 준비 작업의 협조적 종료를 기다린다. 이어서 모든 설치된 installer의 ReleaseAsync를 역순 실행하며, 하나가 실패해도 나머지를 시도하고 Uninstall까지 수행한다.
- ReleaseAsync는 비동기 종료·실행 중 작업 정리를 기다린다. Uninstall은 주입된 참조 제거와 최종 Dispose를 수행한다. 두 단계는 부분 준비·실패에도 동작해야 한다.
- 해제가 시작되면 IsPrepared는 false이고 신규 준비 요청을 거부한다. 같은 ShutdownAsync를 여러 번 호출해도 해제를 중복 실행하지 않는다. 종료 후 root는 재사용하지 않는다.
- 해제 도중 전환 요청이 취소되어도 정리를 끝낸 뒤 씬 진행을 중단한다. 임의의 timeout이나 강제 작업 중단은 추가하지 않았다.
- Unity OnDestroy/종료 콜백은 await할 수 없다. graceful shutdown이 필요하면 root를 파괴하거나 씬을 unload하기 전에 ShutdownAsync를 await한다. 즉시 파괴 시에는 준비 대기를 취소하고 기존 Uninstall로 긴급 정리한다. 늦게 완료된 준비가 IsPrepared를 다시 올리거나 씬을 진행하지 못한다.
- installer는 준비 수명 토큰을 존중하고, 해제 뒤의 늦은 작업이 서비스를 건드리지 않도록 자체 정리해야 한다. 자신의 ReleaseAsync에서 같은 root.ShutdownAsync를 await하면 자기 자신을 기다리므로 금지한다.
- SingletonSceneRoot.Instance의 등록은 객체 수명까지 유지된다. 비동기 종료 후에는 root를 파괴하고, 종료된 서비스에 신규 요청을 보내지 않는다.

## 실패·취소와 표시 보호

- 사용자 선택: 실패·취소 시 씬 진행을 중단하고 가림막을 유지한다. 자동 재시도·화면 복귀는 하지 않는다.
- 시스템 준비 실패는 root의 ShutdownAsync로 부분 상태를 정리한다. 준비 실패와 정리 실패가 겹치면 둘 다 AggregateException으로 전달한다.
- 호출자 대기 취소는 공유 준비를 종료하지 않는다. 전환은 중단되지만 시스템 준비가 계속될 수 있다. 시스템까지 종료하려면 별도로 ShutdownAsync를 호출한다.
- proceedAsync 실패·취소 시 reveal을 호출하지 않는다. reveal 도중 실패·취소되면 취소되지 않은 토큰으로 cover를 다시 await한 뒤 HandleFailure에 전달한다.
- HandleFailure는 실행 중 실패·취소를 한 번 통지하며 예외를 성공으로 바꾸지 않는다. 이미 취소된 토큰이나 재진입 등 실행 전 거부는 콜백 없이 호출자에게 전달한다. 연출 복구나 오류 callback까지 실패하면 원래 오류와 함께 전달한다. UI callback 자체의 실패까지 완전한 화면 보호를 보장할 수는 없다.
- flow 하나를 전환 소유자에 보관하고 재사용한다. 실행 중 중복 호출은 거부한다. 서로 다른 flow 인스턴스를 만들어 같은 화면을 동시에 제어하지 않는다.
- 가림막·오류 UI는 해제되는 씬 root보다 오래 살아 있어야 한다. 최초 진입 가림막은 씬/상위 UI에서 기본적으로 표시해 Install/Awake 사이 첫 프레임 노출을 막는다. 가림막 콜백은 표시와 함께 입력 차단을 적용하고, reveal 완료 시 해제한다.

## 검증

실제 Red/Green, 전체 테스트, 반복 Play, 컴파일·Console 및 보존 파일 해시는 `doc/validation/async-scene/`에 기록한다. 테스트는 연출 callback의 완료 순서와 상태를 검사하며 실제 화면의 페이드·입력 UX를 확인한 증거는 아니다. 실제 UI 연출, 다른 프로젝트 가져오기와 Player 빌드는 후속 통합 단계다.

- 실제 Red: 신규 12건 실패, reveal 복구 추가 검사 2건 실패. [초기](validation/async-scene/red-play.json), [복구](validation/async-scene/red-reveal.json)
- Green 전체: EditMode 41/41, PlayMode 56/56, 실패·skip 0. [EditMode](validation/async-scene/green-edit.json), [PlayMode](validation/async-scene/green-play.json)
- 반복 Play 10/10: 두 root의 준비·해제와 원래 씬·옵션 복원 확인. [결과](validation/async-scene/reload-check.json)
- 컴파일 완료·상태 ready·최종 Console 오류 0건. 실패 경로의 예상 예외 8건 보존. [실행·복구·보존 기록](validation/async-scene/execution.json)
