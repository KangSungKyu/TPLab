# 표준 idx·generic 데이터 테이블 검증

2026-10-06. 기준 main/7cd8955, 작업 codex/generic-data-tables. [구현 순서](../../DATA_TABLE_GENERIC_IMPLEMENTATION.md), [idx 계약](../../DATA_TABLE_IDX_DRAFT.md), [DTO/테이블 계약](../../DATA_TABLE_MAPPING_DRAFT.md)을 따른다. 기존 InitScene·미추적 SceneTemplateSettings 변경은 보존했다.

## 실제 실행

| 검사 | 실행 / 통과 / 실패 / skip | 증거 |
|---|---|---|
| codec Red | 13 / 1 / 12 / 0 | [JSON](codec-red.json) |
| codec Green | 13 / 13 / 0 / 0 | [JSON](codec-green.json) |
| 표준 등록·조회 Red | 17 / 0 / 17 / 0 | [JSON](standard-red.json) |
| 표준 등록·조회 Green | 17 / 17 / 0 / 0 | [JSON](standard-green.json) |
| binding/FK Red | 18 / 2 / 16 / 0 | [JSON](binding-red.json) |
| binding/FK Green | 18 / 18 / 0 / 0 | [JSON](binding-green.json) |
| 표준 비동기 수명 | 3 / 3 / 0 / 0 | [JSON](async-green.json) |
| 자산·root 연결과 ResourceManager 회귀 | 20 / 20 / 0 / 0 | [JSON](resource-root-green.json) |
| 최종 전체 EditMode | 110 / 110 / 0 / 0 | [JSON](full-EditMode.json) |
| 최종 전체 PlayMode | 87 / 87 / 0 / 0 | [JSON](full-PlayMode.json) |

Red는 최소 컴파일 가능한 미구현 본문을 호출해 실행 실패를 확인한 결과다. codec의 프로젝트 예제 1건과 binding 검사에서 이미 동작하는 factory/resource 규칙 2건은 Red에서도 통과했다. 첫 짧은 IdxCodecTests CLI filter가 반환한 0건은 제외하고 완전한 namespace/class로 실행했다. 컴파일 실패·실행 0건을 Red/Green으로 사용하지 않는다.

표준 등록/조회의 Green 이후 Attribute Optional·프로젝트 TypeConverter 검사 1건을 추가했으며 최종 전체 실행에서 확인했다. 신규 EditMode는 49건(13+18+18), PlayMode는 5건(비동기 3+native/root 2)이다. 전체 EditMode 110건 중 Addressables DocExample RequiredTest 1건은 MyLab 자체 테스트가 아니다.

codec-red.json은 실제 CLI 반환의 집계와 실패 원인을 기록했다. 나머지 JSON은 보관한 동일 실행의 txt에서 추출했다. txt는 터미널 출력으로 JSON 시작 전 안내문이 있을 수 있으며 행 끝 공백만 제거했다. 원본 실행 수·이름·오류 문맥은 유지한다. 별도 XML을 생성했다고 주장하지 않는다.

## 확인한 동작

- 기본 Parts/Stride·양방향/전체 형식·uint.MaxValue와 overflow, 프로젝트 선택 3요소·LocalType별 같은 테이블 PK.
- 표준/임의 int·string·uint 키 혼합, 기존 manual 0 key 허용, 표준 idx 0/종류/완전 PK 검사, 동일 DTO의 다른 종류.
- 정확한 DTO Get/TryGet 실패 구분, 미준비/종료 상태·router 예외 전파, 이전 snapshot의 행 참조 보존.
- 상속 Id·Name·ClassMap·Optional·TypeConverter, 필수 header-only·대소문자·중복 열/PK·변환/인용 오류, 행/테이블 hook.
- 명시적 interface/default/concrete table의 동일 인스턴스와 legacy Rows, 잘못된 binding·등록 동결, null/예외/성공·실패 후보 factory 재사용 거부.
- 같은 완전한 후보의 nullable FK·필수 null·0·전체 형식·예상 종류·실제 PK, 자기·순환 관계, 재로드 대상 제거 시 전체 이전 세대 유지.
- 공유 재로드·개별 취소·worker source의 메인 스레드 parsing/hook·worker query 거부·Dispose 후 늦은 표준 공개 거부.
- native Addressables catalog/provider/handle에서 표준 CSV TextAsset 공급, Singleton root의 준비 후 씬 진행, 잘못된 idx에서 진행 중단·가림막 callback 유지·데이터 owner 종료 후 자산 한 번 해제. 기존 SceneOwned/manual 연결도 보존했다.

비동기/root 검사는 기존 수명 동작에 표준 경로를 연결한 추가 검증이며 별도 Red를 주장하지 않는다. 실제 가림막을 렌더링한 최종 시각 UX 증거가 아니다. source가 native 자원을 소유하며 데이터 테이블은 관리 DTO/컨테이너만 보관한다.

## 환경과 재현

MyLab 기존 Editor PID 23176, Unity 6000.3.18f1/Connector 0.4.1을 사용했다. [컴파일 완료](compile-after.txt), [Editor 상태](status-after.txt), [예상 테스트 경로 로그](console-before-clear.json), Console을 비우고 정상 상태에서 [오류·경고 0건](console-after.json)을 분리해 기록한다. Test Runner의 예상 로그 계약도 전체 실행에서 통과했다.

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab editor refresh --compile
unity-cli --project C:\Users\PC\Projects\MyLab test --mode EditMode
unity-cli --project C:\Users\PC\Projects\MyLab test --mode PlayMode
```

두 실행은 같은 Editor에서 순차 수행한다. 집중 검사는 `--filter MyLab.Core.Tests.IdxCodecTests`처럼 완전한 클래스명을 사용한다. CLI 안내문·오류를 JSON으로 직접 오인하지 않는다.

[입력 hash](test-inputs.json)는 UTF-8 BOM 제외·CRLF→LF 정규화한 최종 MyLab C#·asmdef·manifest/lock·Unity 버전 SHA-256이다. [정적 검사](static-checks.json)는 문서 링크/색인, asset/meta·GUID, 범위·변경 전 사용자 hash 보존을 기록한다. 최종 전체 실행 이후 runtime·테스트·assembly·package·settings 입력은 변경하지 않았다.

## 완료 경계

이번 변경은 관리 데이터 API·검증과 기존 씬 수명 연결이며 사용자 직접 확인이 필요한 새 UI/scene asset 변경이 없다. 필수 자동 검사·원격 상태·해당 커밋 검사를 확인한 뒤 승인된 정책에 따라 통합한다. GitHub workflow/check가 없으면 CI 통과라고 기록하지 않는다. Git 수행·현재 통합은 [최종 회고](../../retrospectives/2026-10-06-06-generic-data-table-validation.md)와 최종 보고를 따른다.

소비 프로젝트 가져오기·최소 예제·Player/IL2CPP에서 CsvHelper DTO 매핑/코드 보존·실제 원격 bundle·대용량 CSV 프레임 예산·최종 시각 UX는 미실행이다. 이는 이후 코어 배포 검증 범위이며 이번 MyLab 내부 동작을 전체 배포 호환성 완료로 확대하지 않는다.
