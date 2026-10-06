# CSV Editor importer 구현 검증

2026-10-06. 기준 main/19b5770, 작업 codex/data-table-importer. [현재 계약·시작 방법](../../DATA_TABLE_IMPORTER_DRAFT.md), [단위 회고](../../retrospectives/2026-10-06-10-data-table-importer.md)를 따른다. 기본 Text/Resource 타입을 runtime에 재도입하지 않았다.

## 실제 실행

| 검사 | 실행 / 통과 / 실패 / skip | 증거 |
|---|---|---|
| 생성기 Red → Green | 20 / 1 / 19 / 0 → 20 / 20 / 0 / 0 | [Red 요약](red-generator.json) · [Green](green-generator.json) |
| 소유권/writer Red → Green | 27 / 20 / 7 / 0 → 27 / 27 / 0 / 0 | [Red 요약](red-writer.json) · [Green](green-writer.json) |
| typed 서비스 Red → Green | 32 / 27 / 5 / 0 → 32 / 32 / 0 / 0 | [Red 요약](red-service.json) · [Green](green-service.json) |
| 기존 DTO/hook/FK Red → Green | 37 / 33 / 4 / 0 → 37 / 37 / 0 / 0 | [Red 요약](red-existing-fk.json) · [Green](green-existing-fk.json) |
| profile 변경·작업 스레드 Red → Green | 39 / 37 / 2 / 0 → 39 / 39 / 0 / 0 | [Red 요약](red-thread-profile.json) · [Green](green-thread-profile.json) |
| 최종 집중 검사·I/O 복구 추가 | 40 / 40 / 0 / 0 | [JSON](green-final-targeted.json) · [터미널](green-final-targeted.txt) |
| 최종 전체 EditMode | 154 / 154 / 0 / 0 | [JSON](full-EditMode.json) · [터미널](full-EditMode.txt) |
| 최종 전체 PlayMode | 87 / 87 / 0 / 0 | [JSON](full-PlayMode.json) · [터미널](full-PlayMode.txt) |

Red는 실제 실패 실행을 확인했다. 초기 CLI 실패는 stderr로 반환되어 PowerShell Tee-Object에 원문이 저장되지 않았다. 따라서 Red JSON은 해당 도구 출력의 실행 수·오류 원인을 요약한 자료이며 전체 stderr 원문/실패 이름 목록으로 주장하지 않는다. Green/full JSON은 보관한 같은 실행의 stdout에서 추출했다. 마지막 full 실행은 코드 스타일 정리까지 포함한 소스에 대응한다. 전체 EditMode에는 외부 Addressables DocExample RequiredTest 1건이 포함된다.

## 실제 Editor 자동화 확인

[native-editor.json](native-editor.json): 확인 **11개**, 실제 typed 전체 검증 **6회**, 실패 0, 검증용 자산/setting.asset 제거 확인. 정확한 MyLab Editor PID **23176**, Unity **6000.3.18f1**, Connector **0.4.1**에서 실행했다.

- 실제 CSV/스키마 추가 → 자동 생성 → C# 컴파일/domain reload → 새 DTO/table의 generic 조회·전체 검증.
- 데이터/header 순서 수정 시 C# 쓰기 없음, 중복 PK는 생성 파일 변경 전 거부.
- CSV 이동 GUID 유지·새 경로 연결, 삭제 진단·생성 소스 유지, 삭제 후 재생성 연결.
- ValidateOnly의 GUID 보존, Disabled에서 입력 변경 시 typed 실행 없음, Disabled에서도 수동 검증 가능.
- 의도적인 `#error` 소스를 검사 전용 root에 추가해 컴파일 오류가 stale typed 검증을 차단함을 확인. 제거·재컴파일 후 검증이 재개됐다. compiler diagnostics는 Unity의 복구 컴파일 때 사라져 [native 종료 Console](console-native-after.json)은 빈 목록이다. 이를 오류 주입 전 로그로 해석하지 않는다.

첫 native 실행의 [실패 기록](native-editor-failed.json)은 6확인/3typed 후 CSV 재생성 연결 문제다. Unity가 삭제 GUID의 이전 경로를 반환하여 이미 없는 이동 경로를 읽었다. 존재·현재 GUID 일치를 함께 확인하고 successful validation에서 새 InputGuid를 기록하도록 수정한 뒤 같은 전체 시나리오가 통과했다. 실패 실행도 자기 자산을 제거했다.

검사 진입점은 `Assets/MyLab/Validation/Editor/DataTableImportEditorCheck.cs`다. 새 타입을 컴파일 전후 연결하는 reflection은 이 검증 코드에만 사용한다. production 등록은 명시적 typed factory이며 native 검사는 기존 활성 설정/검사용 root가 있으면 중단한다. 현재 프로젝트에는 활성 설정 자산이나 게임별 입력/생성 타입을 남기지 않았다.

## 입력과 정적 검증

- [test-inputs.json](test-inputs.json): 최종 MyLab C#·asmdef·manifest/lock·Unity 버전 60개 입력의 SHA-256, UTF-8 BOM 제외·CRLF→LF 정규화. 이후 실행 코드는 변경하지 않았다.
- [static-checks.json](static-checks.json): 문서 상대 링크/anchor·색인, asset/meta·GUID, 변경 범위·공백, baseline 유지 입력과 기존 사용자 파일 bytes.
- [format-check.json](format-check.json): 설치된 Roslyn `dotnet format whitespace`를 신규 C# 8개 복사본에만 적용했다. 프로젝트 설정을 추가하지 않았고 task-owned Temp/ImporterFormatting은 제거했다.
- [compile-final.txt](compile-final.txt), [status-after.txt](status-after.txt), [예상 실패 테스트 Console](console-before-clear.json), Console을 비운 뒤 [오류·경고 0](console-after.json). CLI idle HTTP 응답 메시지는 reload 연결 경고이며 C# 오류가 아니다.
- 공유 reader 추출은 기존 수동/표준 등록 경로 모두에 적용했다. 기존 arbitrary PK/uint0·ClassMap/converter·snapshot·취소·root/자산 수명 테스트를 포함한 full 회귀가 통과했다.
- writer I/O 실패 검사는 manifest 경로에 directory 충돌을 만들어 소스 반영 후 실제 IOException을 유도하고 원본 복구·stage 제거를 확인한다. 모든 OS/디스크 장애의 원자성을 보장하는 증거는 아니다.

## 재현과 완료 경계

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab editor refresh --compile
unity-cli --project C:\Users\PC\Projects\MyLab test --mode EditMode --filter MyLab.Core.Tests.DataTableImporterTests
unity-cli --project C:\Users\PC\Projects\MyLab exec 'MyLab.Core.Tests.DataTableImportEditorCheck.Run(); return null;' --allow-async
# native 종료와 자기 자산 제거를 확인한 뒤 같은 Editor에서 순차 실행
unity-cli --project C:\Users\PC\Projects\MyLab test --mode EditMode
unity-cli --project C:\Users\PC\Projects\MyLab test --mode PlayMode
```

이번 범위의 필수 동작은 자동 확인했으며 새로운 사용자 직접 실행 gate는 없다. Inspector는 기본 직렬화 UI와 상태/수동 버튼을 사용한다. 시각적 UX acceptance·소비 프로젝트·Player/IL2CPP는 미실행이며 공용 코어 전체 배포 완료로 주장하지 않는다. JSON 행 로더·타입 rename migration·임의 PK 생성은 미구현 후속 범위다. GitHub 검사 유무와 통합은 최종 Git 확인에서 구분하고 workflow/check 0개를 CI 통과로 쓰지 않는다.
