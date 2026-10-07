# 외부 제공용 README·API 문서 작성 지침

2026-10-07. TPLab을 다른 Unity 프로젝트에 제공할 때 사람이 사용·판단할 문서와 AI가 정확하게 통합·수정할 문서를 함께 관리한다. 이 문서는 **작성 지침**이다. 아래 예약 경로의 문서 전체가 이미 작성됐거나 외부 배포가 완료됐다는 뜻은 아니다.

## 문서의 역할과 경로

| 대상 | 경로 | 역할 |
|---|---|---|
| 사람용 README | `README.md` | 기능 선택, 설치, 첫 사용, 지원 범위와 다음 문서 안내 |
| 사람용 API | `doc/api/README.md`, `doc/api/<Module>.md` | API 색인과 기능별 계약·사용 예제·오류 처리 |
| AI용 README | `doc/ai/README.md` | 필요한 모듈·문서·소스만 찾을 수 있는 짧은 통합 안내 |
| AI용 API | `doc/ai/api/README.md`, `doc/ai/api/<Module>.md` | 정확한 심볼과 계약을 일정한 항목으로 기록한 기능별 참조 |

`<Module>` 이름은 양쪽 API 문서에서 일치시킨다. API 색인은 실제 존재하는 문서만 연결한다. AI 문서도 사람이 검토할 수 있는 UTF-8 Markdown으로 작성한다. 첫 문서화에서는 기존 [기능 명세 색인](INDEX.md)을 활용하고, 같은 계약을 새 파일에 무조건 복제하지 않는다. 기존 명세가 사람용 API 역할을 충분히 수행하면 API 색인에서 해당 문서로 연결하고 소유 위치를 명시할 수 있다.

## 공통 사실과 상태

- 실제 public API의 선언과 XML 주석을 API 사실의 근거로 삼고 구현·호출 흐름·해당 테스트와 대조한다. 구현과 주석·문서가 다르면 차이를 해결하거나 명시하며, AI 문서만 바꿔 동작이 맞다고 판단하지 않는다.
- 설계 명세는 장문의 계약과 결정 근거를 소유한다. 사람용 API는 이해와 사용에 필요한 설명을 제공하고, AI용 API는 그 계약을 짧게 정리해 원문·소스에 연결한다. 한 동작에 서로 다른 정책을 두지 않는다.
- 각 기능 문서에는 대상 배포 버전(있을 때), 근거 source commit 또는 tag, 구현 상태, 검증 환경·범위·증거, 알려진 제한을 기록한다. source commit은 해당 코드를 포함하는 커밋이며 문서 자체의 최종 SHA를 미리 적을 필요는 없다.
- 구현 상태는 `Implemented / Proposed / Deprecated`, 검증 상태는 `Verified / Partial / NotRun`처럼 별도 항목으로 표시한다. 초안 API를 사용 가능한 API 목록에 섞지 않는다. 사용자 수락 대기도 자동 검증 성공과 구분한다.
- 확인한 Unity·package·플랫폼 조합과 지원을 약속한 범위를 구분한다. 한 환경의 compile/Player 결과로 다른 Unity 버전·IL2CPP·플랫폼 지원을 주장하지 않는다. 실행0건·과거 결과·CI 미구성을 이번 통과로 표현하지 않는다.

## 사람용 README

처음 읽는 개발자가 다음 내용을 순서대로 따라 사용할 수 있어야 한다.

1. 프로젝트 목적, 제공 모듈, 모듈별 필수·선택 의존성. Core만 가져오는 방법과 Input/Samples/Editor 포함 조건을 구분한다.
2. 확인된 Unity·package 버전과 설치/가져오기 방법. 필요한 폴더·assembly·`.meta`·라이선스 자료, 사용 프로젝트가 설정할 항목을 적는다. Unity CLI 같은 개발 도구를 runtime 설치 의존성으로 오해하게 만들지 않는다.
3. 최소 실행 예제 하나와 기대 결과. 원본 asset 지정, 생성/주입, 준비, 사용, 해제의 흐름을 보여준다. 필요한 Inspector 설정·씬 root 조건도 포함한다.
4. API 문서와 예제 위치, 검증 범위·지원 제한, 업그레이드 시 호환성 변경과 migration 안내.
5. TPLab의 배포 라이선스/정책과 외부 의존성의 출처·버전·라이선스 자료 위치. 아직 결정하지 않은 정책은 미정으로 적고 외부 의존성 라이선스를 TPLab 자체 라이선스로 대신하지 않는다.

README에는 전체 메서드 목록이나 작업 회고를 쌓지 않는다. 사용에 필요한 정보만 두고 API·검증·변경 내역으로 연결한다. 사용 프로젝트가 제공할 DTO·키 생성/추출 규격·validator·UI/callback과 코어가 제공할 기능을 명확히 구분한다.

## 사람용 API 문서

기능별로 책임과 사용 흐름을 먼저 설명한 뒤 정확한 타입·메서드·프로퍼티·event·설정 항목을 기술한다. 다음 사항이 적용되면 생략하지 않는다. 해당 사항이 없으면 `없음`, 확인하지 못했으면 `미확인`으로 적는다.

- namespace/assembly, public signature와 generic 제약, 매개변수의 null·범위·기본값, 반환값·결과 상태·예외.
- 생성·초기화·준비·공개·종료 순서, 소유/대여 구분, lease/handle/구독의 반환·해제 담당자, 중복 해제와 종료 후 호출 동작.
- 메인 스레드 조건, 비동기 완료/공유 await 규칙, 재진입·동시 요청·caller/owner 취소, 실패 시 부분 결과 정리·기존 값 보존 여부.
- 설정·직렬화·ID/GUID·JSON/CSV 형식, 필요한 dependency, 프로젝트가 교체할 callback/validator와 그 호출 시점·금지 동작.
- 최소 정상 사용과 대표 실패/취소 처리 예제, 기존 사용자가 달라져야 하는 호환성 변경. 예제에서 획득한 자원의 정리도 보여준다.

전체 예제는 필요한 using·설정·선행 조건을 포함하고 실제 코드와 대조한다. 설명용 발췌·프로젝트 제공 placeholder는 그렇게 표시한다. 실행 가능한 예제라고 표기하려면 해당 source 기준으로 compile/실행을 확인하고 증거를 연결한다. 문서 전용 예제를 실행 검증한 것처럼 적지 않는다.

## AI용 README와 API 문서

AI용 README는 전체 저장소를 읽지 않고 필요한 계약으로 이동하는 입구다. 모듈/namespace/assembly/소스/사람용 API/AI용 API의 대응표, 최소 설치·주입·준비·정리 순서, 작업별 읽는 순서, 프로젝트가 제공해야 하는 항목을 짧게 기록한다. 공개 API와 테스트 fixture·Samples·Editor 도구를 구분한다.

AI용 API는 아래 항목 이름을 일정하게 사용한다. class 전체를 복사하지 않고 public 심볼과 소비자가 지켜야 할 불변 조건에 집중한다. overload는 각각 정확한 signature를 적고 모호한 `Get` 같은 이름만으로 식별하지 않는다.

```text
Module / Namespace / Assembly
SourceRevision / SourcePath / HumanContract
ImplementationStatus / ValidationStatus / Evidence
Symbol / Signature / Constraints
Inputs / Outputs / Errors
Ownership / Lifecycle / Threading
Concurrency / Cancellation / FailureCleanup
Configuration / ExtensionPoints
RequiredSequence / ForbiddenUsage
Example / Compatibility / Limitations
```

항목의 값은 구현에서 확인한 내용만 쓴다. API 이름·타입·enum·ID·default를 추측하거나 언어 모델이 이해하기 쉬운 이름으로 바꾸지 않는다. 꼭 필요한 호출 순서를 순차 목록으로 적고 같은 manager의 소유권·취소 계약이 다른 API에도 적용되면 공통 계약으로 연결한다. 게임별 UI·데이터·부팅 정책을 코어 기본 규칙으로 일반화하지 않는다.

AI 문서는 소비 프로젝트의 권한을 부여하지 않는다. 이 저장소의 Git 승인·개인 경로·세션 ID·PC 종료 지시를 다른 프로젝트에 옮기지 않는다. 통합 시 해당 프로젝트의 사용자 지시와 작업 지침을 따른다. 개발 작업 규칙은 [AGENTS.md](../AGENTS.md)가 소유하며 사용 API 문서에 복제하지 않는다.

## 갱신과 검증

- public API·기본값·의존성·소유권·취소·오류·설정·지원 범위가 바뀌면 같은 단위에서 XML 주석과 해당 모듈의 사람/AI API 문서를 함께 갱신한다. 설치/첫 사용/읽는 순서에 영향이 있으면 양쪽 README와 색인도 갱신한다. 내부 구현만 바뀌고 외부 계약이 같으면 그 이유를 회고에 적고 무관한 문서를 재생성하지 않는다.
- 문서 리뷰는 정확한 signature·상태·호출/해제 순서·예제·내부 링크·변경 범위·공백을 확인한다. 동작 변경의 필수 자동 검증과 사용자 UX 확인은 기존 프로젝트 gate를 따른다. 순수 문서 지침 변경에는 Unity 테스트를 추가하거나 실행0건을 통과로 기록하지 않는다.
- 외부 제공 전에는 제공할 source/tag 기준으로 설치·최소 예제·문서 링크를 다시 확인하고 사람/AI 문서, 필요한 소스·의존성·라이선스 자료를 함께 제공한다. 제공본 안에서 해결되는 상대 경로를 사용한다. 로컬 절대 경로, `Temp` 자료, 내부 track/회고, 개인 세션이나 인증 정보에 필수 사용법을 의존시키지 않는다.
- 문서 누락·미검증·지원 제한은 제공본에 표시한다. 배포 도구·자동 API generator·JSON catalog는 실제 반복 필요가 확인될 때 검토한다. 이번 지침 작성 때문에 package·runtime·배포 시스템을 추가하지 않는다.

기존 기능의 네 문서 작성·외부 제공본 정리는 별도 단위 작업에서 진행한다. 해당 작업이 끝나기 전에는 이 지침 추가만으로 문서화나 외부 배포 준비가 완료됐다고 보고하지 않는다.
