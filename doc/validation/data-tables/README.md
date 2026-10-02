# DataTableManager 검증

2026-10-02. `main / 2ac76ef1e4f1d18e61216b121628b3a444849749`에서 `feature/data-table-manager`를 생성했다. 사용자 수정 InitScene과 미추적 SceneTemplateSettings는 보존하고 stage에서 제외했다. [기능 계약](../../DATA_TABLE_MANAGER.md)을 따른다.

## 실행 결과

| 단계 | 실행 / 통과 / 실패 / skip | 증거 |
|---|---|---|
| CSV·snapshot 계약 Red | 19 / 0 / 19 / 0 | [JSON](red-edit.json), [XML](red-edit.xml): 미구현 API의 실제 실패 |
| 기본 계약 Green | 19 / 19 / 0 / 0 | [JSON](green-edit.json), [XML](green-edit.xml) |
| 미사용 열 검증 Red | 20 / 19 / 1 / 0 | [JSON](unused-column-red.json), [XML](unused-column-red.xml): 읽지 않은 열의 인용 오류를 공개함 |
| 미사용 열 검증 Green | 20 / 20 / 0 / 0 | [JSON](unused-column-green.json), [XML](unused-column-green.xml) |
| 비동기 계약 | 8 / 8 / 0 / 0 | [JSON](async-green.json), [XML](async-green.xml) |
| 자산/root 연결·자산 회귀 | 18 / 18 / 0 / 0 | [JSON](resource-integration-green.json), [XML](resource-integration-green.xml) |
| 최종 전체 EditMode | 61 / 61 / 0 / 0 | [JSON](full-EditMode.json), [XML](full-EditMode.xml) |
| 최종 전체 PlayMode | 82 / 82 / 0 / 0 | [JSON](full-PlayMode.json), [XML](full-PlayMode.xml) |

신규 테스트는 EditMode 20건, 비동기 PlayMode 8건, 기존 ResourceManager fixture의 데이터 root 연결 2건이다. EditMode 전체에는 설치된 Addressables DocExample RequiredTest 1건이 포함되며 MyLab 자체 테스트로 세지 않는다. 구현 전에 CSV 계약 19건의 실패를 실제 실행했다. 비동기·root 추가 검사는 이미 구현된 수명 계약을 확인한 검사이며 별도 Red로 주장하지 않는다.

최초 테스트 assembly 컴파일에서 CsvHelper 참조가 누락되었다. TestAssemblies의 precompiled DLL 참조를 CsvHelper.dll·nunit.framework.dll로 명시한 뒤 실행한 결과를 위 Red로 사용한다. 패키지·DLL·plugin 설정은 변경하지 않았다. 미사용 열 오류는 CsvHelper가 필드 접근 시 검증하는 특성에서 발생하여 모든 열에 기존 CsvHelper 필드 검증을 적용하고 Green을 확인했다.

저장소 공백 검사에 맞춰 red-edit.xml의 stack trace 행 끝 공백만 제거했다. 실행 수·결과·예외 문맥은 유지하며 원본은 로컬 Temp/DataTableValidation/red-edit-raw.xml에 보관했다. 최종 전체 XML은 실행 원본 그대로다.

## 검증 범위

- BOM·따옴표 쉼표·여러 줄·이스케이프·InvariantCulture 소수, 추가 열·빈 테이블 허용과 필수 header·행 값 정책.
- 중복 header·키, null CSV/행/키, 열 수·타입·인용 오류와 테이블/행 오류 문맥.
- 여러 후보의 단일 공개, FK 실패·공급자 실패/취소 시 기존 snapshot 보존, 성공 재시도와 이전 snapshot 유지.
- 공유 로드, 개별·전체 대기자 취소, 사전 취소, 구성 동결, 종료 후 재요청 거부와 늦은 완료 미공개.
- worker 공급자 완료 후 메인 스레드 파싱/검증, worker API 호출의 거부와 상태 보존.
- native Addressables catalog·provider·handle에서 CSV TextAsset 로드, 준비 후 씬 진행, 데이터 소유자 종료 후 자산 해제.
- CSV 변환 오류 시 씬 진행 중단·가림막 유지·root rollback·자산의 한 번 해제.

실제 가림막을 렌더링한 증거가 아니라 callback과 root 수명의 실행 증거다. ResourceManager 기존 fixture의 임시 native catalog·runtime JSON·provider를 재사용하며 완료 시점만 제어한다. 테스트 locator·provider·임시 파일과 PlayerPrefs 경로는 기존 정리/복원 계약을 따른다.

## 컴파일·Console·정적 확인

MyLab의 기존 Editor PID 42616, Unity 6000.3.18f1, Connector 0.4.1을 사용한다. [Editor 상태](status-after.txt), [컴파일](compile-after.txt), [예상 실패 경로 로그](expected-test-errors.json), Console을 비운 뒤 정상 Editor [오류 0건](console-after.json)을 분리해 저장한다. Test Runner의 LogAssert도 예상하지 않은 오류를 검사한다.

[정적 검사](static-checks.json)는 새 asset/meta 짝·GUID 중복·문서 링크/색인·변경 범위·사용자 변경 hash를 기록한다. [입력 hash](test-inputs.json)는 최종 실행의 MyLab C#·asmdef·manifest/lock·Unity 버전 텍스트를 UTF-8 BOM 제외·CRLF→LF로 정규화한 SHA-256이다. 최종 전체 실행 이후 코드·assembly·패키지·설정은 변경하지 않는다. JSON은 같은 실행의 최신 완료 TestResults.xml에서 추출한다.

## 완료 경계

이 단계는 관리 데이터·검증·수명 연결이다. UI·scene asset·프로젝트 설정을 변경하지 않았고 필요한 동작을 자동 검증하므로 병합에 필요한 사용자 수동 확인 항목은 없다. 별도 소비 프로젝트 가져오기·최소 예제 실행·Player/IL2CPP·실제 bundle·대용량 CSV 프레임 예산·최종 시각적 UX는 후속 단계이며 위 결과로 완료를 주장하지 않는다.

GitHub workflow·해당 push 커밋의 checks/status는 별도로 확인한다. workflow가 없으면 CI 통과로 기록하지 않는다.
