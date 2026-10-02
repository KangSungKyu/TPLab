# MonoSingleton 계약

`MyLab.Core.Lifecycle.MonoSingleton<T>`는 Unity 메인 스레드에서 명시적으로 생성한 컴포넌트 하나를 등록한다. 모든 manager가 상속해야 하는 기반은 아니다. Cashier의 등록·정리 hook을 참고하고 초기화 실패, 중복 제거 범위와 반복 Play 수명을 보완했다. Cashier 코드·패키지는 복사하지 않았다.

## API와 소유권

- `Instance`: 초기화에 성공한 소유자 또는 `null`. 검색·자동 생성하지 않는다. 초기화 중, Play 밖, 애플리케이션 종료 중에는 공개하지 않는다.
- `IsInitialized`: 해당 컴포넌트의 초기화 완료 여부. 비활성화·재활성화만으로 소유권을 바꾸거나 초기화를 반복하지 않는다.
- `PersistAcrossScenes`: 기본값 `false`로 씬 수명을 따른다. `true`를 override하면 root GameObject와 자식들이 유지된다. 자식에 붙인 영속 Singleton은 부모를 이동시키지 않고 오류로 거부한다. [Unity DontDestroyOnLoad](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Object.DontDestroyOnLoad.html)
- `OnSingletonInitialize()`: 동기 초기화 hook. 성공 후에만 `Instance`를 공개한다. 재진입으로 만든 같은 타입의 두 번째 컴포넌트는 초기화하지 않는다.
- `OnSingletonShutdown()`: 소유자 파괴 또는 다음 Play의 상태 초기화에서 호출한다. 시작된 초기화가 실패했을 때도 부분 상태 정리를 위해 한 번 호출한다. `Instance`를 먼저 비운 뒤 정리한다. 정리 중 같은 타입의 재생성은 거부한다.

첫 소유자를 유지하고 중복 컴포넌트만 비활성화·파괴한다. 중복 GameObject의 다른 컴포넌트와 자식은 보존한다. 빈 GameObject의 제거가 필요하면 생성자가 맡는다. 초기화·정리 예외는 각각 Console에 기록하고, 실패한 컴포넌트는 제거한다. 정상 소유자는 명시적으로 다시 생성할 수 있다.

파생 타입은 `T`에 자신의 타입을 지정한다. `Awake`, `OnEnable`, `OnDestroy`, `OnApplicationQuit`을 숨기지 않고 제공된 hook으로 초기화·정리를 구현한다. 초기화가 아직 진행 중인 자기 자신을 `Instance`로 조회하지 않는다. 비동기 작업·취소·리소스 해제는 소비자의 hook이 소유하며, 이 기반은 비동기 준비 완료를 보장하지 않는다.

```csharp
using MyLab.Core.Lifecycle;

public sealed class AppRoot : MonoSingleton<AppRoot>
{
    protected override bool PersistAcrossScenes => true;
}
```

씬의 root GameObject에 `AppRoot`를 붙인다. 실행 중 `AppRoot.Instance`로 성공한 소유자를 조회한다. 별도의 일반 C# Singleton은 이번 단계에 추가하지 않았다.

## 반복 Play

Domain Reload가 꺼지면 static 상태를 수동 초기화해야 한다. nongeneric `SingletonRuntime`의 `SubsystemRegistration`에서 등록 상태·종료 상태를 초기화한다. `BeforeSceneLoad`에서는 이미 알려진 Singleton 타입의 활성 씬 컴포넌트를 확인하여 Scene Reload가 꺼진 경우도 등록을 복원한다. 씬 검색은 Play 진입 시에만 수행한다. [Unity Domain Reload](https://docs.unity3d.com/6000.3/Documentation/Manual/domain-reloading.html), [초기화 실행 시점](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html)

같은 컴포넌트 ID가 보존되어도 Unity가 인스턴스 필드를 복원할 수 있어, 검증은 Play별 실제 초기화·정리 횟수를 기준으로 한다. Editor 전용 자동 검사 `SingletonReloadCheck`는 네 가지 Domain/Scene Reload 조합에서 각각 두 번 Play하고, 종료 중 신규 등록 거부와 다음 Play 복구를 별도로 두 번 확인한다. 원래 씬 구성·옵션을 저장·복원하고 임시 씬을 삭제한다. 저장되지 않은 씬 변경이 있으면 실행을 거부한다.

재실행은 다른 테스트가 끝난 동일 Editor에서 다음 명령으로 시작한다. 결과 파일의 완료를 확인하기 전 다른 Editor 명령이나 테스트를 실행하지 않는다.

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab exec 'MyLab.Core.Tests.SingletonReloadCheck.Run(); return "started";'
```

결과는 `Temp/Singleton/reload-check.json`이다. `Success=true`, `CompletedPlayChecks=10`, `SettingsRestored=true`와 각 진입·종료 관찰 기록을 확인한다. 이는 NUnit 테스트 건수와 별도로 보고한다. Test Framework 1.6.0의 중첩 Play 검사에서 실행기가 Domain Reload 후 사라져 실제 Editor 이벤트를 이용했다. Unity CLI/Connector·Test Framework 패키지는 수정하지 않았다.

## 검증 (2026-10-02)

- 동일 MyLab Editor: Unity 6000.3.18f1, Connector 0.4.1, PID 42616.
- TDD Red: stub의 PlayMode 14건 중 통과 2, 실패 12, skip 0. [실행 결과](validation/singleton/red-play.json)
- Green 전체 EditMode: 38/38, 실패·skip 0. [실행 결과](validation/singleton/green-edit.json)
- Green 전체 PlayMode: 31/31 = 기존 프리팹 17 + Singleton 14, 실패·skip 0. 초기화 공개 순서, 중복·타입 독립성, 비활성화, 소유자 파괴·재생성, 초기화·정리 예외, 재진입, 씬 해제, 영속성·부모 계층 보존을 확인했다. [실행 결과](validation/singleton/green-play.json)
- 반복 Play 자동 검사: 10/10, 옵션·씬 복원 완료. [실행 결과](validation/singleton/reload-check.json)
- Editor 컴파일 완료, 최종 상태 ready. 실패 경로에서 `LogAssert.Expect`로 검증한 [예외 로그 6건](validation/singleton/expected-test-exceptions.json)을 보존했다. 예상 밖 오류는 없었고, 확인 후 Console을 비운 상태의 [오류는 0건](validation/singleton/console-errors.json)이다. 초기 실패 검사와 실행기 문제의 로그는 Temp/Singleton에 별도 기록했다.
- [입력 SHA-256](validation/singleton/test-input-sha256.json)과 실행 환경·결과 복구 경로는 [검증 기록](validation/singleton/execution.json)에 있다. 검증한 runtime·테스트 소스와 함께 커밋한다.
- 사용자 직접 확인이 필요한 항목은 없다. Player 빌드·IL2CPP·다른 Unity 버전·소비 프로젝트 가져오기는 후속 단계이며 이번 통과로 주장하지 않는다. 기존 미추적 `ProjectSettings/SceneTemplateSettings.json`은 보존하고 변경에 포함하지 않았다.
