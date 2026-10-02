# ResourceManager 검증

2026-10-02. 기준 branch `main` / `bbdb5ec4001a934e90ff89348980cb5c11b86778`에서 `feature/resource-manager`를 생성했다. 기존 사용자 변경 `Assets/Scenes/InitScene.unity`와 `ProjectSettings/SceneTemplateSettings.json`은 보존·stage 제외했다.

## 실행 결과

| 단계 | 실행 / 통과 / 실패 / skip | 증거 |
|---|---|---|
| 자산 API Red | 8 / 0 / 8 / 0 | [red.json](red.json): 미구현 API·입력 검증 실패 |
| 자산 API Green | 8 / 8 / 0 / 0 | [asset-green.json](asset-green.json) |
| installer Red | 13 / 10 / 3 / 0 | [installer-red.json](installer-red.json): 누락된 주입·준비 계약 |
| installer Green | 13 / 13 / 0 / 0 | [installer-green.json](installer-green.json) |
| 스레드 경계 Red | 1 / 0 / 1 / 0 | [thread-red.json](thread-red.json): worker Dispose가 상태를 변경함; 실제 CLI 출력 기록 |
| 기능별 Green | 16 / 16 / 0 / 0 | [targeted-green.json](targeted-green.json), [XML](targeted-green.xml) |
| 최종 전체 EditMode | 41 / 41 / 0 / 0 | [full-EditMode.json](full-EditMode.json), [XML](full-EditMode.xml) |
| 최종 전체 PlayMode | 72 / 72 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json), [XML](full-PlayMode.xml) |

최종 PlayMode 72건에는 기존 56건과 새 ResourceManager 16건이 포함된다. EditMode 41건 중 1건은 설치된 Addressables DocExample의 RequiredTest이며 MyLab 자체 테스트로 세지 않는다. 최종 전체 실행은 ResourceManager가 native catalog를 직접 초기화하도록 강화한 fixture를 포함한다.

최초 8건 실행은 Addressables runtime 설정이 없어서 SetUp에서 실패했다. 계약 Red로 세지 않고, 임시 binary catalog·runtime JSON을 구성한 뒤 위 Red를 다시 실행했다. 프로젝트 설정이나 Addressables 자산을 영구 생성하지 않았다. 초기화 중 잠시 사용한 PlayerPrefs 경로는 finally에서 복원하며 fixture locator·임시 파일·provider는 테스트 종료 시 정리한다.

Unity CLI가 Domain Reload 중 `unity editor has stopped`를 보고한 실행은 재실행하지 않고 같은 PID의 새 완료 JSON/XML을 확인했다. CLI가 정상 응답한 실행에서는 완료 JSON 파일이 추가되지 않기도 하므로 최종 JSON은 해당 실행의 신선한 TestResults.xml에서 추출했다. CLI 종료 코드만으로 통과를 판단하지 않았다.

## 확인한 계약

- 같은 key·타입의 동시 로드·캐시 공유, 타입 충돌, 사전 취소·개별 취소·전체 대기자 취소.
- provider 실패 후 재요청, 초기화 dispatch 실패 후 재요청, 초기화 중 종료 후 readiness 미공개.
- 완료·진행 중 종료, 공유 Shutdown 대기, 늦은 완료의 한 번 해제, 종료 후 요청 거부, 서로 다른 scope의 native 참조 독립성.
- worker thread의 Initialize/Load/Dispose/Shutdown 거부와 상태 보존.
- SceneOwned root의 필수 자산 대기·가림막 callback 순서, Singleton root의 prefab clone 생성·pool 종료 후 자산 해제.
- 준비 실패의 rollback·가림막 유지, 호출자 취소의 진행 중단과 명시적 소유자 종료, root 파괴 후 늦은 결과 정리.

native Addressables 초기화·ResourceProvider·ResourceLocator·AsyncOperationHandle을 실행했다. 원격 네트워크나 실제 bundle 대신 테스트 provider의 완료 시점만 제어한다. 비동기 catalog 다운로드 실패·플랫폼별 content build 실패를 재현한 증거로 확대하지 않는다.

## 컴파일·Console·정적 확인

- MyLab 기존 Editor PID 42616 / Unity 6000.3.18f1 / Connector 0.4.1 사용: [status-after.txt](status-after.txt).
- Editor 컴파일 완료: [compile-after.txt](compile-after.txt). 별도 Player 빌드 증거가 아니다.
- 예상된 실패 경로 로그는 [expected-test-errors.json](expected-test-errors.json), [full-expected-test-errors.json](full-expected-test-errors.json)에 분리했다. Test Runner의 LogAssert도 예상하지 않은 로그를 검사했다. Console을 비운 후 컴파일·정상 Editor 상태의 오류 0건: [console-after.json](console-after.json).
- 새 asset/meta 5쌍, MyLab GUID 중복 0, runtime의 UnityEditor/UI 의존 없음, package/settings 변경 없음. [static-checks.json](static-checks.json).
- 보존한 InitScene SHA-256: `12C5C4095B1806434C0D7758B7F91CDD830BFB5C78F1F7E25E331038F823B045`.
- [test-inputs.json](test-inputs.json)은 최종 검증 코드·asmdef·manifest/lock·Unity 버전의 SHA-256이다. UTF-8 BOM 제외·CRLF를 LF로 정규화한 텍스트를 해시하여 Git checkout 줄바꿈에 독립적이다.

## 완료 경계

이 변경은 공용 자산 cache와 root 수명 연동이다. UI·실제 scene asset을 변경하지 않았고 callback 순서와 pool 수명을 자동으로 확인했으므로 이번 병합에 사용자 수동 확인 항목은 없다. 별도 소비 프로젝트 가져오기, 실제 원격 bundle·catalog 갱신, Player/IL2CPP, 최종 가림막의 시각적 UX는 후속 통합 단계이며 이번 통과로 주장하지 않는다.

GitHub CI 구성·해당 커밋의 checks/status는 push 후 별도로 확인한다. workflow가 없으면 CI 통과로 기록하지 않는다.
