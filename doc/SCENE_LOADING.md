# 씬 대상과 로더 계약

2026-10-06. 작업 트랙 P0 확정 구현 계약이며, 실제 구현·검증 상태는 [track](SCENE_TRANSITION_TRACK.md)을 따른다. ResourceManager의 일반 자산 캐시 계약은 변경하지 않는다.

## 책임과 대상

- 리소스 영역의 SceneTarget은 Build Scene 또는 Addressable을 명시한다. 등록 여부로 로더를 추측하거나 실패 시 다른 backend로 fallback하지 않는다.
- Build Scene은 Assets/.../*.unity 전체 경로를 사용한다. Inspector의 SceneAsset 선택을 경로로 저장하며 기존 Bootstrap 문자열 설정/API를 유지한다.
- Addressable은 해당 씬의 전체 경로와 고유 주소 또는 AssetReference의 runtime key를 사용한다. Editor는 선택 참조에서 경로를 저장하고 주소/참조가 그 SceneAsset에 매핑되는지 확인한다. Player에서 SceneAsset/AssetDatabase를 사용하지 않는다.
- ISceneLoader의 검사와 LoadAsync는 로드 방식만 담당한다. NativeSceneLoader와 AddressableSceneLoader는 기존 Unity SceneManager/Addressables API를 사용한다. GameSceneManager는 source에 따라 주입된 로더를 선택한다.
- GameSceneManager가 모드·유지 집합·전환 조건·가림막·root 준비/종료·active 선택·등록·실패/취소 정리를 소유한다. 로더가 이 정책을 실행하거나 공용 root를 종료하지 않는다. 파생 씬은 첫 범위에서 동일 asset의 동시 중복 인스턴스를 거부한다.

## 개별 로드 결과와 해제

- LoadedScene은 실제 Scene 인스턴스, 대상과 개별 해제 작업을 연결한다. Addressables native handle은 로더 내부에 감춘다. 같은 key로 모든 씬 결과를 공유하는 자산 캐시를 만들지 않는다.
- 성공한 native 로드 결과는 경로가 예상과 다르더라도 소유권을 manager에 먼저 전달한다. manager가 실제 Scene 경로와 요청 대상을 확인한 뒤 오류·root 정리·잔여 씬 진단을 처리한다. 특히 Single의 마지막 씬을 로더가 해제하지 못한 채 결과 없이 예외로 버리지 않는다. Addressables InternalId는 FullPath/Filename/GUID 등 catalog 설정에 따라 달라지므로 전체 경로와 같다고 강제하지 않는다.
- 관리자는 성공 결과를 독점 보유하고 해당 root의 ShutdownAsync가 끝난 뒤 결과의 UnloadAsync를 요청한다. 동일 결과의 해제 대기는 공유하고 성공한 해제는 반복 호출에 무해하다. 해제 실패는 호출자에게 남기며 해제되지 않은 씬을 성공 상태로 숨기지 않는다.
- 호출자 token은 전환 await만 취소한다. native 로드를 강제 중단하지 않으며 소유자 취소 시 로드 완료 결과까지 소유한 뒤 후보 root/씬을 정리한다. 로드 실패의 native handle은 로더가 해제하고, 성공 후 root 준비 실패의 결과는 manager가 정리한다.
- 마지막 일반 씬의 별도 unload 제한, 외부 직접 unload/Single, 공용 root 이동·파괴 등 기존 소유권 검사를 유지한다. Single 자동 해제 뒤의 handle 유효성과 명시 해제를 구분하며 중복 Release하지 않는다.
- allowSceneActivation=false를 시스템 준비 대기로 사용하지 않는다. 씬 activation과 Scene Root 준비·화면 준비·게임 진행 허용은 구분한다.

## 사전 검사와 검증

진행률 전달은 현재 로더 계약에 없다. [로딩 화면 초안](SCENE_LOADING_PRESENTATION_DRAFT.md)에서 기존 ISceneLoader를 보존하는 선택적 progress 경계·StageRatio의 의미·UI observer 실패 후 native 결과 소유권 확보를 제안한다. 진행률 확장과 신규 UI 흐름은 아직 구현하지 않았다.

Build 대상은 실제 Player build scene list 포함 여부, Addressable은 설치된 Addressables 설정의 단일 씬 매핑을 검사한다. 양쪽 모두 실제 SceneAsset/root/공용 수명 모순을 확인하며 주소 씬에 Build Scene 포함을 강제하지 않는다.

P0는 target 잘못된 입력, 명시적 로더 선택, 결과 해제 공유/실패, caller/owner 취소, late completion, 실제 Native/Addressables 씬 load/unload와 기존 Bootstrap 회귀를 검증한다. 실제 콘텐츠 빌드·Player/원격 다운로드·최종 Inspector/UX 확인은 P6 증거 및 사용자 확인과 별개다. 설치된 Addressables 2.9.1의 [씬 로드](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/LoadingScenes.html)와 [해제 API](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/api/UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync.html)를 따른다.
