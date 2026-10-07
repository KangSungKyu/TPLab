# TPLab

2026-10-07 이름 통일: namespace/assembly와 소스 경로는 `TPLab` / `Assets/TPLab`을 사용한다. [변경 안내](doc/TPLAB_NAMING.md)에서 현재 이름·경로 규칙과 검증 기록을 확인한다.

Unity 프로젝트에서 재사용하는 공용 코어다. C# 객체·prefab pooling, 선택적인 Singleton/scene root 수명, Addressables 자산, CSV 테이블, 씬 전환과 Input System wrapper를 제공한다. 게임별 데이터·UI·저장 정책은 사용하는 프로젝트가 정의한다. 프로젝트·namespace·assembly 표기는 TPLab으로 통일한다. 소스는 `Assets/TPLab`에 있으며 로컬 checkout 폴더 이름은 설치 환경에 따라 다를 수 있다.

[사람용 API](doc/api/README.md) · [AI용 README](doc/ai/README.md) · [AI용 API](doc/ai/api/README.md) · [예제](Assets/TPLab/Samples/SceneTransitions) · [기능 명세](doc/INDEX.md)

| 모듈 | 제공 기능 |
|---|---|
| [Pooling](doc/api/Pooling.md) | 일반 C# reference type ObjectPool와 Unity PrefabPool adapter |
| [Lifecycle](doc/api/Lifecycle.md) | MonoSingleton, SceneOwned/Singleton root, installer와 비동기 준비·종료 |
| [Resources](doc/api/Resources.md) | Addressables 공유 자산 cache와 owner 해제 |
| [DataTables](doc/api/DataTables.md) | CSV·uint idx typed 조회·binding·FK·전체 snapshot 검증, 임의 PK 경로 |
| [SceneManagement](doc/api/SceneManagement.md) | Bootstrap·Single/Additive·주 씬 교체·중첩 구역·root 조건·진행 표시·선택적 로딩 화면/진행 대기 callback |
| [Input](doc/api/Input.md) | Input System runtime clone·layer·리바인딩·override JSON |
| [Editor](doc/api/Editor.md) | CSV/schema importer·설정 asset·씬/root 사전 검사 |

씬 전환 진행률, 선택적 로딩 callback과 자동·수동 진행 대기, 그리고 프로젝트 소유 bar·팁·버튼을 연결한 sample UI가 구현됐다. 2026-10-07 사용자가 로딩 UI를 PlayMode로 확인했다. [현재 계약](doc/SCENE_LOADING_PRESENTATION_DRAFT.md)과 [API](doc/api/SceneManagement.md)를 따른다.

## 배포 준비

첫 `0.0.1`의 [배포 규격·dev-build track](doc/DISTRIBUTION_PIPELINE.md)을 작성했다. Core·선택적 Input/Editor의 UPM `.tgz`와 생성 전용 `tplab/` 폴더를 설계했으며, 실제 자동화·패키지·Release는 아직 제공하지 않는다. 현재 설치는 아래 소스 가져오기를 따른다. 저장소는 [public](https://github.com/KangSungKyu/TPLab)이며 외부 제공을 허용한다. TPLab 자체 구현은 MIT로, 제3자는 원문 조건으로 제공한다.

## 가져오기

확인된 환경은 Unity **6000.3.18f1**, Windows Mono다. UPM 배포 package는 제공하지 않으므로 필요한 소스 폴더를 `.meta`와 함께 가져오고 사용하는 프로젝트의 assembly·설정을 확인한다. 프로젝트 전체 Assets/ProjectSettings/manifest를 덮어쓰는 방식으로 설치하지 않는다.

| 선택 | 가져올 소스 | 의존성 |
|---|---|---|
| Core | `Assets/TPLab/Core`, `Assets/Plugins/CsvHelper` | UniTask **2.5.11**, Addressables **2.9.1**, 포함 CsvHelper **33.1.0** DLL |
| Input 추가 | `Assets/TPLab/Input/Runtime` | Core + Input System **1.19.0** |
| Editor 추가 | `Assets/TPLab/Editor` | Core + Addressables Editor, Unity Newtonsoft.Json **3.2.2** |
| 전환 Samples 추가 | `Assets/TPLab/Samples/SceneTransitions` | Core + Input + uGUI **2.0.0**; 예제 씬/Build Settings·Addressables 구성 필요 |

Core asmdef는 자산/씬 기능과 같은 assembly이므로 폴더 전체를 가져오면 Addressables·UniTask·CsvHelper가 필요하다. Input은 별도 assembly로 선택할 수 있다. `Tests`, `Validation`, fixture는 runtime 설치 대상이 아니다. importer는 프로젝트가 작성할 DTO/validator/profile을 필요로 한다.

1. UPM에서 Addressables를 설치하고 UniTask Git URL `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11`을 추가한다. 고정 버전은 [manifest](Packages/manifest.json)와 [lock](Packages/packages-lock.json)을 참고한다.
2. 선택한 소스와 CsvHelper DLL/`.meta`/라이선스 자료를 가져온다. 사용하는 프로젝트의 asmdef에서 `TPLab.Core`, 선택 시 `TPLab.Core.Input`을 참조한다.
3. Input을 선택하면 Input System을 설치하고 Active Input Handling을 Input System에 맞춘다. action asset은 프로젝트가 소유하며 wrapper가 복제한다. legacy native input은 TPLab 지원 정책 밖이다.
4. 씬의 최상위 root에 SceneOwnedRoot 또는 SingletonSceneRoot을 선택하고 필요한 installer를 연결한다. 준비 완료 후 시스템을 공개하고, owner가 종료를 담당한다. [Lifecycle](doc/api/Lifecycle.md)을 따른다.
5. 씬 전환에는 명시적인 BuildScene/Addressable target과 실제 등록을 설정한다. Bootstrap 첫 씬 → 게임 씬 Additive를 권장하지만, 영속 공용 root 조건을 만족하면 Single도 선택할 수 있다. [SceneManagement](doc/api/SceneManagement.md)를 따른다.

Unity CLI/Connector **0.4.1**과 Test Framework **1.6.0**은 이 저장소의 개발·검증 도구다. 소비 runtime 설치에는 필요하지 않다. 가져오기 도구의 allowlist와 실행 방법은 [tools](tools/README.md)에 있다.

## 첫 사용

다음은 순수 C# pooling의 설명용 발췌(NotRun)다. 필요한 using과 반환/owner 해제를 보여주며, 이 문서 블록 자체의 별도 compile/실행은 하지 않았다. 대여 중 capacity가 모두 사용되면 TryRent가 false다. onReturn이 목록을 초기화하므로 다음 대여에서 이전 데이터가 남지 않는다.

```csharp
using System.Collections.Generic;
using TPLab.Core.Pooling;

using (var pool = new ObjectPool<List<int>>(
    () => new List<int>(), 8, onReturn: list => list.Clear()))
{
    if (pool.TryRent(out var list))
    {
        try { list.Add(42); }
        finally { pool.Return(list); }
    }
}
```

실행 확인된 통합 예제는 [sample controller](Assets/TPLab/Samples/SceneTransitions/Runtime/SceneTransitionSampleController.cs)와 [consumer smoke](tools/core-consumer/templates/ConsumerSmoke.cs)다. sample은 `TPLab > Scene Transitions > Open Additive / Open Single`에서 실행하고 `Restore Original Setup`으로 복원한다. 이미 생성된 예제의 사용과 sample builder의 생성은 구분한다. 다른 프로젝트에서 예제 씬을 사용할 때는 해당 프로젝트의 build list·Addressables와 root/installer를 설정해야 한다. 데이터 schema 예시는 [템플릿](doc/templates/data-tables/README.md)을 프로젝트 namespace로 옮겨 변경한다.

## 검증과 호환성

입력 모듈의 과거 P4 검증은 source `9305b5dd0f730636f431fd5d19a1c9102fdc3bed` 기준이다. 현재 로딩 표시 P4에서는 EditMode **271/271**, PlayMode **253/253**(실패0·skip0), Additive/Single Windows Mono build와 Player(각 12/12), Input 포함 consumer Editor build 1회와 Player run 1회가 성공했다. 자세한 실행/범위는 [P4 증거](doc/validation/scene-loading/p4/README.md)와 [입력 증거](doc/validation/input-system/p4/README.md)를 확인한다.

P2의 targeted 36/36과 그 이전 Core 252/252·224/224는 역사적 결과다. 현재 P4 전체 회귀와 두 모드 build/Player 및 Input 포함 consumer smoke는 성공했다. 2026-10-07 사용자의 PlayMode 확인으로 최종 사용자 gate를 완료했다. 개별 해상도·장치별 확인 결과는 제공되지 않았다. [최종 통합](doc/validation/scene-loading/integration/README.md)에 따라 main에 반영한다.

다른 Unity 버전·플랫폼·IL2CPP, 물리 게임패드/touch의 개별 UX, 모든 abrupt 종료 조합은 미검증이다. CI는 현재 미구성이며 자동 CI 통과로 표현하지 않는다. 변경 시 [문서 갱신 지침](doc/DOCUMENTATION_GUIDE.md)에 따라 XML 주석과 사람/AI 문서를 같은 작업에서 갱신한다. 업그레이드 전에는 소비 프로젝트에서 가져오기·compile·예제 실행을 다시 확인한다. 기존 Text/Resource schema는 core에서 제거되어 프로젝트 템플릿으로만 제공한다.

## 라이선스

TPLab 자체 코드·문서는 [MIT License](LICENSE)로 제공하며 외부 프로젝트 사용·수정·재배포를 허용한다. [제3자 고지](THIRD_PARTY_NOTICES.md)의 라이선스/저작권 원문은 별도로 유지한다. TPLab의 MIT는 제3자 구성 요소를 재허가하지 않는다. [UniTask](doc/licenses/UniTask-LICENSE.txt), [CsvHelper](Assets/Plugins/CsvHelper/LICENSE.txt), 개발 도구 [Unity CLI](doc/licenses/UnityCli-LICENSE.txt)를 함께 확인한다. Unity package의 배포 조건은 설치한 해당 package의 LICENSE를 따른다. 이 문서 작성은 외부 제공본 배포 완료를 뜻하지 않는다.
