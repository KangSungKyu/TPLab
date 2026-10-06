# 데이터 테이블 예시 템플릿

Text·Resource는 프로젝트별 필드 구성이 달라질 수 있으므로 코어 기본 타입에서 제외했다. [예시 소스](../../../Assets/MyLab/Tests/Fixtures/DataTableTemplates.cs)의 TextRow/TextDataTable, ResourceKeyRow/ResourceKeyDataTable을 프로젝트의 출발점으로 사용한다.

소스 namespace는 `MyLab.Examples.DataTables`다. MyLab에서는 기존 TestFixtures assembly에서 컴파일해 EditMode·PlayMode·native CSV/root 테스트로 검증한다. `MyLab.Core` assembly나 일반 Player runtime에 예시 타입을 포함하지 않으며 테스트 assembly를 제품 의존성으로 추가하지 않는다. 별도 샘플 package·assembly나 자동 등록은 없다.

## 프로젝트에서 사용

1. 필요한 DTO와 테이블을 프로젝트 runtime 폴더에 복사한다. 아래 소스에서 두 쌍 중 필요한 것만 선택해도 된다.
2. namespace를 프로젝트 namespace로 변경하고 클래스 이름·CSV 필드·행/테이블 검증을 프로젝트 규격에 맞춘다. 기존 이름도 프로젝트 namespace에서 사용할 수 있다.
3. 프로젝트 installer에서 router·종류·이름·CSV 공급자·매 시도 새 테이블 factory를 등록한다. 이미 등록된 이름·종류를 덮어쓰지 않고 최초 구성에서 사용할 구현을 선택한다.

코어가 제공하는 기반은 `IDataRow`, `DataRow`, `IDataTable<TRow>`, `CsvDataTable<TRow>`다. `DataRow`의 Id는 CSV idx를 읽으며 프로젝트가 미리 생성한 완전한 uint PK다. Text/Resource 필드나 종류 번호를 manager가 가정하지 않는다. 기본 규격을 따르지 않는 임의 PK는 기존 수동 Register/GetTable을 사용한다.

```csharp
// 프로젝트 namespace로 복사한 TextRow/TextDataTable과 프로젝트 CSV 공급자를 사용한다.
manager.RegisterIdxRouter(new DecimalIdxCodec(stride));
manager.RegisterTable<TextRow, TextDataTable>(
    textDataType, "texts", readTextCsvAsync, () => new TextDataTable());
manager.RegisterTable<ResourceKeyRow, ResourceKeyDataTable>(
    resourceDataType, "resources", readResourceCsvAsync, () => new ResourceKeyDataTable());
var snapshot = await manager.LoadAsync(cancellationToken);
TextRow text = snapshot.Get<TextRow>(nameIdx);
```

namespace는 코어에 `MyLab.Core.DataTables`, 복사한 타입에 프로젝트 namespace를 사용한다. 공급자·Stride·종류 번호·idx 값은 소비 프로젝트가 정한다. 동일 종류에는 하나의 테이블만 등록한다. `new` factory는 재로드와 실패 재시도를 포함한 매 시도마다 새 객체를 제공한다.

## 예시 CSV와 변경 지점

Text 예시는 `idx,text`다. 빈 text를 허용하며 다국어 등 다른 필드가 필요하면 DTO의 `[Name]` 또는 table의 `ConfigureMapping` ClassMap을 변경한다.

```csv
idx,text
1001,ready
```

Resource 예시는 `idx,key`다. 예시 ValidateRow는 공백 key를 거부한다. key가 실제 Addressables 자산인지, 해당 자산이 준비됐는지는 ResourceManager 준비 흐름에서 확인한다. 이 예시는 자산을 로드하거나 handle을 소유하지 않는다.

```csv
idx,key
2001,Assets/Project/Example.prefab
```

위 idx는 Stride=1000, 종류 1/2를 선택한 예시다. 필수/선택 열·converter·FK와 오류 정책은 [매핑 계약](../../DATA_TABLE_MAPPING_DRAFT.md), [idx 계약](../../DATA_TABLE_IDX_DRAFT.md)을 따른다. 공개 DTO는 읽기 전용으로 취급하고 관련 참조는 같은 snapshot에서 읽는다.

이번 템플릿 분리의 [검증 기록](../../validation/data-table-templates/README.md)과 [회고](../../retrospectives/2026-10-06-07-data-table-templates.md)를 확인한다. 소비 프로젝트 복사·Player/IL2CPP 실행은 이번 MyLab 테스트와 별도의 후속 검증이다.
