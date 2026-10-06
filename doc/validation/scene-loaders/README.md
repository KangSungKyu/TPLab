# P0 명시적 씬 로더 검증

2026-10-06. 기준 track acb636c933f2eea29f46705d233f4e0dba436965, 작업 codex/game-scenes-p0-loaders. 기존 MyLab Unity 6000.3.18f1 / Connector 0.4.1 / PID 23176에서 실행했다. [계약](../../SCENE_LOADING.md), [회고](../../retrospectives/2026-10-06-17-scene-loaders.md)를 따른다.

| 실제 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 로더 계약 Red | 16 / 1 / 15 / 0 | [red-edit.json](red-edit.json) |
| Editor mapping Red | 8 / 3 / 5 / 0 | [red-editor.json](red-editor.json) |
| 로더 계약 Green | 18 / 18 / 0 / 0 | [green-edit-initial.json](green-edit-initial.json) |
| Editor mapping Green | 13 / 13 / 0 / 0 | [green-editor.json](green-editor.json) |
| 실제 native/Addressables Green | 13 / 13 / 0 / 0 | [green-play.json](green-play.json) |
| 최종 전체 EditMode | 213 / 213 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 최종 전체 PlayMode | 128 / 128 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

BuildScene/Addressable 명시 선택, 잘못된 결과의 소유 후 정리, 실제 SceneProvider Additive/Single, 같은 key 실패 후 재요청, native 늦은 완료 취소, external Single 이후 정리, 공유 unload 완료/실패를 검사했다. Addressables 의존성 provider의 release 관찰도 포함한다. 실패 SceneOperation의 내부 reference count 자체를 독립 측정한 증거는 아니다. 내부 handle은 결과 밖에 노출하지 않으며 ResourceManager 자산 cache는 변경하지 않았다.

Editor는 Build 목록 밖 Addressable 허용, 미등록/잘못된 key·GUID·path·subobject·중복 address·catalog inclusion, 누락 root·Singleton 충돌을 검사했다. 테스트 settings는 비영속 주입 인스턴스이며 사용자 Addressables 설정을 생성·수정하지 않았다. Unity가 null AssetReference를 빈 인라인 값으로 직렬화하는 경우 Address 설정을 무효화하지 않도록 보완했다. 패키지 추가 없이 설치된 UniTask.Addressables assembly 참조를 명시했다.

[첫 전체 Play 실패](iteration-full-PlayMode.json)는 128 / 126 / 2 / 0이었다. 기존 ResourceManager 테스트가 제거한 locator를 먼저 등록해 이미 초기화된 Addressables와 격리했고, SceneProvider 의존성 fixture를 실제 IAssetBundleResource 계약으로 수정했다. [의존성 release 확인 실패](iteration-dependency-release.json) 13 / 12 / 1 / 0에서는 SceneProvider가 완료 통지 뒤 load handle을 해제한다는 실행 순서를 확인했다. loader는 해당 callback이 반환한 뒤 공유 unload 완료를 공개하며, 최종 즉시 release 검사는 통과했다. 실패 iteration은 성공 gate로 사용하지 않는다.

[실제 native gate](native-editor.json)는 BuildPipeline 및 Play가 비영속 공용 root의 Single을 거부한 결과다. BuildResult=Unknown은 사전 BuildFailedException에 의한 중단이며 성공 Player 빌드가 아니다. [예상 native 로그](native-expected-console.json)를 보존했다. helper의 delayCall 기록이 진행되지 않아 idle/Play 거부 상태를 읽은 뒤 기존 기록·정리 메서드를 호출하고 등록 callback을 제거했다. fixture만 정리했고 사용자 씬을 저장하지 않았다.

최종 전체 테스트의 예상 오류 10개를 [보존](expected-test-console.json)한 뒤 clear하여 [최종 Console](final-console.json) 오류·경고 0과 [Editor ready](final-status.txt)를 확인했다. CLI domain reload 연결 단절 시 해당 프로세스 PID와 fresh 실행 결과만 사용했다. 연속 실행의 잠깐 playing/discovery 상태는 기존 Editor의 ready 복귀를 최대 30초 기다리도록 검증 도구에서 처리했다. 다른 Editor나 과거 결과로 대체하지 않았다.

[입력 hash](test-inputs.json)는 최종 소스·assembly·fixture·의존성·도구를, [보존 hash](preserved-inputs.json)는 사용자 dirty 4개와 EditorBuildSettings의 시작 raw bytes를 확인한다. 두 Python self-check와 정적 verifier를 실행했다. 정적 검사는 Unity 실행을 대신하지 않는다. 정확한 push commit의 CI 확인은 track 통합 보고에 남기며 CI 미구성을 CI 성공으로 표시하지 않는다.

```powershell
python tools/run_unity_tests.py --project C:/Users/PC/Projects/MyLab --mode EditMode --output Temp/MyCheck/edit.json
python tools/run_unity_tests.py --project C:/Users/PC/Projects/MyLab --mode PlayMode --output Temp/MyCheck/play.json
python tools/verify_validation.py --evidence doc/validation/scene-loaders
```

연속 전환·수명 tree·정의/조건은 후속 Phase다. 성공 Player·소비 프로젝트·실제 cover/input UX는 미검증이며 최종 gate로 남긴다. main과 작업 브랜치 삭제는 최종 명시적 사용자 확인까지 보류한다.
