# UIContext P0 증거

2026-10-08. 계약·정적 의존성 조사 단계이며 동작 구현 증거가 아니다.

- 기준: main8ce768d → 설계852ee960, track/P0 branch 생성. 사용자 source/설정6개 [raw hash](protected-inputs.json) 보존.
- [Editor 상태](editor-state.json): 실제 원본 PID24376, Unity6000.3.18f1/Connector0.4.1, ready·컴파일/Play/업데이트 false·InitScene dirty false·Console0.
- 기본 runtime 의존성은 TPLab.Core/UniTask/UnityEngine.UI, Editor는 UI+Editor 한정, tests는 UI/UniTask/uGUI+TestAssemblies와 직접 사용하는 의존성만 포함한다.
- Input runtime은 별도 adapter 경계로 하고 UI base에 Unity.InputSystem/TPLab.Core.Input을 강제하지 않는다. 기존 Sample InputSystemUiScope를 현재 공용 API로 설명하지 않는다.
- 기존 Core 단일 asmdef의 실제 참조는 UniTask/UniTask.Addressables/Unity.Addressables/Unity.ResourceManager이며 CsvHelper DLL도 Core 설치에 포함된다. Pool 직접 코드에는 plugin 의존이 없으나 Core 설치와 분리된 package라는 뜻은 아니다.
- 근거: 실제 Assets/TPLab Core/Input/Tests asmdef, Packages manifest/lock, ProjectSettings ProjectVersion, 기존 Lifecycle/ResourceManager/Pool/Input source. 이번 구현에는 그 파일의 이동·버전 변경을 포함하지 않는다.
- Unity 테스트/Player/Profiler 실행0건. 구현 Red/Green은 P1 이후 실제 명령/동일 Editor/nonzero 결과로 보존한다.
