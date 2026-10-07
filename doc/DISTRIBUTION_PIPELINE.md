# TPLab 배포 규격과 dev-build track

2026-10-07. Status: **Proposed / 설계 작성 완료**, PipelineImplementation: **NotImplemented**, PackageValidation: **NotRun**. 첫 목표 버전은 `0.0.1`, 태그는 `v0.0.1`이다. 현재 설치는 [소스 가져오기](../README.md)이며 이 명세의 package·명령·Release는 아직 제공하지 않는다.

설계 기준 main은 `993a0617b8b5253175d9a225432f0aa642d19d3d`다. 기존 코어 기능·이름 변경 검증은 선행 기록이며 실제 배포물 설치 통과를 대신하지 않는다. 이번 범위는 규격·운영·구현 단계 작성이다. 배포 자동화 구현, 개발 소스 이동, 패키지 생성, Unity 테스트, 태그·Release 발행은 후속 단위다.

## 배포 단위

첫 배포 형식은 **UPM `.tgz` 세 개 + 선택적 예제 + 설치 안내**를 권장안으로 정한다. 사용자는 저장소의 **public 전환·외부 제공 허용·의존성 최소 조건 유지**를 승인했다. TPLab 자체 코드/문서는 [MIT](../LICENSE)를 적용하고 [제3자 고지](../THIRD_PARTY_NOTICES.md)를 보존한다. 현재 파일과 Git 이력의 알려진 인증 정보 패턴 검사·라이선스 문서 main 반영 후 public 전환을 완료했고 비인증 조회를 확인했다. [검증 기록](validation/distribution-design/README.md)을 따른다. 의존성 라이선스와 TPLab 라이선스를 혼동하지 않는다.

| 후보 package ID | 개발 원본 | 패키지 안의 대상 | 소비 의존성 |
|---|---|---|---|
| `com.tplab.core` | `Assets/TPLab/Core`, `Assets/Plugins/CsvHelper` | `Runtime/`, `Runtime/ThirdParty/CsvHelper/` | UniTask 2.5.11, Addressables 2.9.1; 포함 CsvHelper 33.1.0 DLL |
| `com.tplab.input` | `Assets/TPLab/Input/Runtime` | `Runtime/` | 같은 버전 Core, UniTask 2.5.11, Input System 1.19.0 |
| `com.tplab.editor` | `Assets/TPLab/Editor` | `Editor/` | 같은 버전 Core, UniTask 2.5.11, Addressables 2.9.1, Newtonsoft.Json 3.2.2 |

package ID는 새 배포 명세의 후보 식별자다. 첫 구현에서 중복·명명 검사를 통과한 뒤 고정한다. C# namespace·assembly 이름 `TPLab.Core`, `TPLab.Core.Input`, `TPLab.Core.Editor`는 유지한다. Input·Editor 설치는 선택이며 Core에 입력·uGUI·URP를 강제하지 않는다. 개발용 Connector·IDE·Unity Test Framework를 소비 runtime dependency에 추가하지 않는다.

씬 전환 예제는 `com.tplab.input`의 opt-in `Samples~/SceneTransitions` 후보로 묶는다. 실제 사용에는 Core·Input·uGUI 2.0.0이 필요하다. 샘플 import만으로 사용자 Input Handling·Build Settings·Addressables·시작 씬을 자동 변경하지 않는다. 데이터 테이블 템플릿은 Editor package의 선택적 sample 후보다. 기존 테스트/fixture·Validation 스크립트는 기본 소비 패키지에서 제외하고 검증 checkout이 소유한다. package 자체의 테스트 구성이 필요해지면 별도 opt-in 경계를 검토한다.

[Unity package layout](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-layout.html)과 [Samples 규격](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-samples.html)을 따른다. `Runtime`·`Editor`의 기존 파일/폴더 `.meta`는 매핑 대상마다 한 번만 복사하며 GUID를 보존한다. 패키지 root·새 생성 문서에는 기존 부모 GUID를 중복 전파하지 않는다. 소스 복사본과 같은 GUID/assembly의 패키지를 한 소비 프로젝트에 동시에 설치하지 않는다.

## 개발 소스와 생성 공간

개발 원본은 계속 `Assets/TPLab`에 둔다. 저장소 루트 하위 **`tplab/`은 빌드 요청 시 생성하는 공간**이다. 활성 개발 checkout과 구별되는 배포 worktree에서 생성하고, P1 구현 때 root 한정 `/tplab/` ignore 규칙을 추가한다. 현재는 ignore 변경·폴더 생성 모두 미실행이다.

```text
<배포-worktree>/
  Assets/TPLab/                  # Git에서 추적하는 개발 원본
  tools/                        # 반복 실행 도구
  tplab/<고유-run-id>/           # 작업별 생성 공간, Git 제외
    packages/
      com.tplab.core/
        package.json
        Runtime/
        README.md
        CHANGELOG.md
        LICENSE.md              # 원본 LICENSE의 MIT 원문
        Third Party Notices.md
        Documentation~/         # 해당 모듈 사람·AI API/설치 문서
      com.tplab.input/
      com.tplab.editor/
    artifacts/
      com.tplab.core-0.0.1.tgz
      com.tplab.input-0.0.1.tgz
      com.tplab.editor-0.0.1.tgz
      SHA256SUMS.txt
      distribution-manifest.json
      INSTALL.md
      VALIDATION.md
```

각 `.tgz`는 `package/` root 아래 한 package의 manifest·소스·문서를 담는다. 압축 내부 절대 경로·`..`·symlink를 허용하지 않는다. 같은 source SHA·version·패키징 규격의 산출물은 동일 hash가 되도록 파일 순서·archive timestamp·권한·gzip metadata를 정규화한다. 실행 시각·PC 경로·run ID는 별도 검증 기록에 두며 package bytes에 넣지 않는다. 정확한 archive bytes 재현 검사는 P1에서 실제 실행한다.

산출물에는 Unity 프로젝트 전체, Library·Temp·Logs·UserSettings·`.git`, 개인 경로·인증·내부 AGENTS/회고·무관한 게임 씬/설정이 들어가지 않는다. 기존 폴더를 재사용하거나 덮어쓰지 않고 새 run ID로 생성한다. 삭제는 기록한 작업별 목록과 경로를 검증한 뒤 수행한다. 정상/실패 상태 모두 필요한 로그·hash를 영속 증거로 옮긴 뒤 임시물을 정리한다.

## 설치와 의존성

첫 배포는 Release에서 받은 `.tgz`를 Package Manager의 **Install package from tarball**로 설치한다. Core를 먼저, 선택한 Input·Editor를 그 다음에 설치한다. 사용자 프로젝트의 package 원본은 유지하고 필요한 manifest 항목만 합친다. [Unity tarball 설치](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-ui-tarball.html)를 기준으로 검증한다.

UniTask의 현재 원본은 Git URL이므로 소비 프로젝트 `Packages/manifest.json`에 직접 고정한다. UPM은 package 간 Git URL dependency를 지원하지 않는다. TPLab package manifest에는 버전 계약을 기록하고 **소비 프로젝트가 실제 공급원을 명시**하도록 설치 안내와 검증 도구가 확인한다. TPLab Input/Editor의 Core dependency 역시 registry가 공급한다고 가정하지 않고 소비 manifest에 Core `.tgz` 공급원을 먼저 지정한다. [Unity Git dependency 제약](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-git.html).

아래는 **후속 설치 예시 / NotRun**이다. 각 PC가 다운로드한 실제 파일 위치로 바꾼다. 설치 대상 프로젝트 root 아래 `Vendor/TPLab/`에 두는 예를 사용하며 경로는 `Packages/manifest.json` 기준이다. URL에 계정 비밀값을 넣지 않는다.

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.11",
    "com.unity.addressables": "2.9.1",
    "com.tplab.core": "file:../Vendor/TPLab/com.tplab.core-0.0.1.tgz"
  }
}
```

Input을 선택하면 같은 위치의 `com.tplab.input-0.0.1.tgz`와 `com.unity.inputsystem: 1.19.0`을, Editor를 선택하면 `com.tplab.editor-0.0.1.tgz`와 `com.unity.nuget.newtonsoft-json: 3.2.2`를 더한다. 실제 공급원/lock·resolved package version을 읽고 일치 여부를 검증한다. 현재 [원본 manifest](../Packages/manifest.json)를 통째로 복사하지 않는다.

Git clone은 개발·패키징에 사용한다. **생성 전용 `tplab/`은 Git tag에 들어가지 않으므로 `?path=tplab/...#v0.0.1` 설치 URL을 제공하지 않는다.** 향후 직접 Git UPM 설치가 필요하면 package manifest가 실제 해당 Git revision에 포함되는 구조를 별도로 정하고 검증한다. 저장소 visibility를 바꾸거나 생성물을 자동 커밋하는 것으로 우회하지 않는다. 첫 배포에 registry 서버·Jenkins·GitHub Actions를 추가하지 않는다.

## dev-build track 운영

브랜치는 commit 흐름을, worktree는 각 PC의 별도 checkout을 담당한다. 같은 PC의 worktree는 Git object/ref를 공유하지만 다른 PC는 자체 clone과 worktree를 만든다. worktree 폴더를 복사하여 이동식 환경으로 제공하지 않는다. source SHA·Unity 버전·의존성 원본·실행 도구를 고정해 어느 PC에서든 같은 절차로 생성한다. Unity 설치·활성화·Windows Mono 모듈과 원격 package 접근 권한은 각 PC가 준비한다. 원격 의존성을 모두 내려받은 뒤의 동작과 완전 오프라인 빌드는 구분한다. [Git worktree 공식 안내](https://git-scm.com/docs/git-worktree).

- P0 명세는 `codex/distribution-design`에서 작성하고 문서 검증 후 기존 승인에 따라 main에 반영한다. 이 문서 변경에 사용자가 Unity로 수락할 새 동작은 없다.
- 구현 시작 시 실제 최신 main·dirty·원격을 확인하고 `codex/dev-build-v0.0.1` track을 만든다. 그 최신 통합 tip에서 `codex/dev-build-v0.0.1-p1-packaging`, `-p2-consumer`, `-p3-candidate`를 필요한 시점에 생성한다. 이번 P0에서는 이 브랜치들과 worktree를 만들지 않는다.
- Phase는 track으로 순차 통합한다. 공용 계약·source mapping·Unity 실행은 부모가 소유한다. 하위 에이전트는 기존 승인 범위에서 독립 조사/문서 검증이 필요할 때만 사용하며 같은 파일/Editor를 동시에 조작하지 않는다.
- `tplab/` 생성은 읽은 checkout의 HEAD가 명시한 40자리 source SHA와 일치하고 추적 파일이 clean일 때만 허용한다. dev-build 단계의 미커밋 소스는 빌드에 섞지 않는다. 다른 작업의 dirty를 stash/reset/clean하지 않고 격리한 checkout에서 작업한다.
- 후보 생성은 track의 **정확한 커밋**을 detached build worktree로 checkout해 수행한다. 생성 산출물 외에 보존할 미추적 작업이 발견되면 중단한다. worktree는 경로·source SHA·역할·상태를 기록하고 소유한 것만 도구의 관리 절차로 정리한다.
- main 통합은 필요한 실제 자동 검사와 남은 사용자 확인을 완료한 뒤 fast-forward 또는 일반 merge로 한다. 최신 main을 반영하면 결과 커밋 기준으로 다시 빌드·검증한다. 이전 후보 산출물을 새 commit의 통과로 재사용하지 않는다.
- 최종 태그는 **검증한 source commit**을 가리킨다. 이후 마감 문서 commit이 추가됐다고 태그를 이동하지 않는다. source commit이 main 이력에 보존됐는지 확인하고 package version·distribution manifest·검증 보고·Release source를 일치시킨다.
- Phase/track 브랜치 정리는 main push·원본 commit 보존·해당 branch tip/원격 일치·worktree 미사용을 확인한 뒤 기존 조건부 삭제 규칙을 따른다. 활성 build worktree나 후보 결과가 필요한 동안 먼저 삭제하지 않는다.

## 최소 실행 도구와 기록

후속 P1은 Python 표준 라이브러리를 우선 사용한다. git checkout/인증은 기존 Git, package 소비 build는 기존 Unity batch 경로를 사용한다. 자동화 도구가 저장소 생성·인증 설정·네트워크 정보 출력·승인되지 않은 publish를 함께 수행하지 않는다. 한 명령의 실패 exit가 후속 build/merge/publish를 막아야 한다.

후보 CLI는 아래와 같다. **`tools/build_distribution.py`는 아직 없으며 다음 단계의 실행 계약안이다.** version/run ID/path/source를 검증하고 파일 읽기·패키지 생성·hash/manifest 생성만 담당한다. Git 브랜치·merge·publish는 해당 Git 단계가 담당한다.

```text
python tools/build_distribution.py --source <절대-clean-checkout>
    --revision <40자리-HEAD> --version 0.0.1 --run-id <고유-ID>
    --output <해당-checkout>/tplab/<고유-ID>
```

`distribution-manifest.json`은 schemaVersion, sourceRevision, version, package별 ID/버전/filename/SHA256, source-to-package mapping hash, 의존성 계약, 필수 검증/정책 상태를 기록한다. 실행 PC/시각/원래 Editor PID·검증 consumer PID·실제 test count·exit와 로그 경로는 별도 검증 evidence에 둔다. MIT/제3자 고지·필수 설치/build 검증을 충족하지 못한 개발 검증본은 `publishable: false`로 표시한다. 라이선스 결정만으로 artifact 설치 검증을 완료로 표시하지 않는다. 새 파일과 증거의 상세 schema는 P1에서 실제 명령 구현과 함께 확정한다.

재사용 대상은 [도구 안내](../tools/README.md)와 기존 도구다. `run_core_consumer.py`는 현재 Assets 소스 allowlist를 복사하므로 배포물 설치 검증을 이미 지원한다고 보고하지 않는다. P2에서 artifact 설치 mode를 추가하거나 공통 consumer 경로를 최소 분리한다. evidence 경로 allowlist도 배포 검증 root로 확장하고 안전성 self-check를 수행한다. 기존 원본 Editor 검증은 기존 Editor에서, 독립 소비 검증은 명시된 별도 batch consumer에서 실행한다. 두 검증을 서로 대체하지 않는다.

## 구현 Phase와 완료 조건

| Phase | 범위 | 완료 조건 | 현재 |
|---|---|---|---|
| P0 규격·운영 | 이 문서, 입구/지침/회고 연결 | 현재 소스/assembly/의존성 대조, 상대 링크·범위·공백·보호 검사 | 완료 / main 반영·public 확인 |
| P1 패키징 | clean SHA→3개 `.tgz`, 문서/라이선스 projection, hash/manifest | 최소 Red/Green: dirty·잘못된 SHA·경로 탈출·기존 output·symlink 거부; 같은 입력의 archive hash 일치; 내용/버전/`.meta`/DLL 검사 | 미구현 |
| P2 실제 설치 | artifact 소비 mode, sample import 경로 수정, Editor importer 수명 | Core만/Input/Editor/전체+Sample 소비 설치·compile·최소 실행, negative 경로, 원본 보호 | 미구현 |
| P3 배포 후보 | commit 고정, 회귀·Windows Mono sample/consumer·문서/정책 gate | source/산출물/결과 일치, 실제 전체 결과 nonzero, 필요한 사용자 확인 완료 | 미실행 |
| P4 첫 Release | main 통합·`v0.0.1`·public Release와 검증된 첨부물 | 정책/필수 gate 충족, source tag·SHA256·버전 일치·다운로드한 실제 첨부물 검증, 브랜치 정리 | 미실행 |

현재 코어의 [PlayMode 수락](SCENE_LOADING_ACCEPTANCE.md)은 선행 기록이다. 패키지 설치/sample import/새 Editor workflow의 사용자 확인이 필요하면 P3 마지막에 모아 실제 실행 방법·기대 결과를 전달한다. 응답이 바로 이어지지 않으면 track 결과를 보존하고 main 통합·Release를 기다린다. 사용자 확인 항목이 없는 변경은 기존 자동 검증 후 병합 정책을 따른다. 시간 경과를 승인으로 보지 않는다.

P2의 필수 수정/관찰은 다음과 같다.

1. [SceneTransitionSamplePaths](../Assets/TPLab/Samples/SceneTransitions/Runtime/SceneTransitionSamplePaths.cs)의 고정 `Assets/TPLab/Samples/SceneTransitions` 경로와 [sample builder](../Assets/TPLab/Samples/SceneTransitions/Editor/SceneTransitionSampleBuilder.cs)의 marker/asset 경로가 UPM의 sample import 위치를 처리해야 한다. 수정은 sample 계층에 한정하고 실제 source 위치와 import 위치에서 정상/중단/원본 복원을 검증한다.
2. [importer 설정](../Assets/TPLab/Editor/DataTables/DataTableImportSettings.cs)의 `Assets/Editor/TPLab/setting.asset`은 소비 프로젝트 소유로 유지한다. [생성 보호](../Assets/TPLab/Editor/DataTables/DataTableGeneratedFiles.cs)는 package 읽기 전용 영역을 허용하지 않고 프로젝트의 Assets 생성 경로를 검증한다. 설정 없음→자동 작업 없음, validator 실패→이전 생성 소스 보존을 artifact 설치 후 확인한다.
3. 배포 README와 사람/AI API는 포함 모듈에 맞춰 선별하고 실제 package 안에서 해결되는 상대 링크로 source/API 경로를 변환한다. 다른 package의 참조는 존재가 보장되는 공식 문서/Release 위치로 연결하거나 설치 안내에서 설명한다. 개발 저장소의 `Assets/TPLab` 링크·Temp·개인 경로를 배포본에 그대로 두지 않는다. 기존 근거는 [문서 지침](DOCUMENTATION_GUIDE.md)이다.
4. 현재 Core PlayMode 테스트 assembly는 sample·Input·uGUI를 함께 참조한다. Core-only artifact 소비 compile/실행과 전체 개발 회귀를 별도로 수행하고, 전체 테스트 폴더를 Core package에 복사해 Input을 우회 강제하지 않는다.

## 정책·Release gate와 실패 처리

사용자는 외부 제공을 허용하고 추가 제한 없이 참조 의존성의 최소 조건을 지키도록 요청했다. TPLab 자체 구현에는 MIT를 적용한다. MIT의 copyright/permission 원문을 보존하고, 변경 내용과 원본 공급원이 다른 제3자에 TPLab MIT를 덮어씌우지 않는다. CsvHelper DLL의 `MS-PL OR Apache-2.0` 선택권·원문 고지·버전·hash를 보존하며 UniTask 등 외부 package의 원문/버전을 안내한다. Unity package는 공식 원본 설치를 유지한다. package manifest의 `license: MIT`는 TPLab 자체 구현의 조건이며 포함된 제3자는 Third Party Notices의 원문 조건을 따른다. 라이선스/고지·필수 검증 누락 시 최종 Release를 발행하지 않는다.

Unity 버전 기준은 정확히 `6000.3.18f1`, 첫 실제 Player 검증 범위는 Windows Mono다. 다른 Unity/플랫폼/IL2CPP 지원은 별도 증거 전에는 주장하지 않는다. 동일 PC의 재현 성공과 실제 다른 PC 실행 성공도 구분한다. `packages-lock.json`은 소비 검증/개발의 resolved 근거이며 사용자 manifest 전체를 덮어쓰는 배포물로 사용하지 않는다.

필수 test/build/설치 실패, source SHA 불일치, 0건·stale·누락 결과, hash/정책 미확정은 **실패/대기**로 기록하고 다음 gate를 진행하지 않는다. 의도한 run의 결과만 읽으며 불명확한 테스트를 재실행해 최초 실패를 지우지 않는다. package를 검증한 뒤 version/source/내용을 수정하면 다시 생성·검증한다.

GitHub Release는 source tag, 3개 `.tgz`, `SHA256SUMS.txt`, 설치/검증 안내와 변경 내역을 연결한다. 태그/Release가 기존에 있으면 자동 교체·삭제하지 않는다. 필요한 첨부물을 준비한 draft 상태와 실제 published 상태를 구분한다. 이번 사용자가 승인한 public 저장소의 Release는 외부 수신자가 다운로드하는 공개 배포다. 저장소 공개와 `v0.0.1` 패키지 발행을 별도 상태로 보고한다. [GitHub Release 절차](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

CI는 현재 미구성이다. [exact commit gate](../tools/check_github_ci.py)는 main 보호·check/workflow 유무를 확인하지만 Unity test/build 실행을 대신하지 않는다. 실패/대기 중의 자동 재시도·장치 종료·예약 실행은 이번 배포 규격에 추가하지 않는다. 각 Phase 결과·source SHA·증거·미완료는 회고/track 상태에 기록하고 문서 검증 완료를 배포 검증 완료로 표시하지 않는다.
