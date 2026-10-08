# TPLab 배포 규격과 dev-build track

2026-10-08. Status: **P4 / 0.0.1 정식 공개·발행 검증 완료**, PipelineImplementation: **P1/P2/P3/P4Implemented**, PackageValidation: **Passed/UserConfirmed**. 태그 `v0.0.1`의 고정 source는 `de879b9396ddae0523bd3ab86939b679b383dd92`다. [정식 Release](https://github.com/KangSungKyu/TPLab/releases/tag/v0.0.1), [P4 증거](validation/distribution-release/README.md), [사용자 최종 확인](DISTRIBUTION_ACCEPTANCE.md)을 따른다. 브랜치와 장치 종료의 실제 마감은 운영 기록으로 구분한다.

최초 설계 기준 main은 `993a0617b8b5253175d9a225432f0aa642d19d3d`, Git URL·예제 분류·차기 범위 보완 기준은 `36e8ffbb0833293474da43396481895e5d8108d0`다. 기존 코어 기능·이름 변경 검증은 선행 기록이며 실제 배포물 설치 통과를 대신하지 않는다. P0는 규격·운영 설계, P1은 패키징, P2는 실제 설치까지 완료했다. 개발 원본은 Assets에 유지한다. 현재 P3는 후보 회귀·최종 확인이며 main/tag/Release 발행은 P4다.

## 배포 단위

사용자가 채택한 첫 배포 형식은 **Git URL 설치 + UPM `.tgz` 세 개 + 선택적 예제 + 설치 안내**다. 두 설치 경로는 같은 후보 커밋의 동일 package 파일을 사용하고 각각 실제 소비 설치를 검증한다. 사용자는 저장소의 **public 전환·외부 제공 허용·의존성 최소 조건 유지**를 승인했다. TPLab 자체 코드/문서는 [MIT](../LICENSE)를 적용하고 [제3자 고지](../THIRD_PARTY_NOTICES.md)를 보존한다. 현재 파일과 Git 이력의 알려진 인증 정보 패턴 검사·라이선스 문서 main 반영 후 public 전환을 완료했고 비인증 조회를 확인했다. [검증 기록](validation/distribution-design/README.md)을 따른다. 의존성 라이선스와 TPLab 라이선스를 혼동하지 않는다.

| 후보 package ID | 개발 원본 | 패키지 안의 대상 | 소비 의존성 |
|---|---|---|---|
| `com.tplab.core` | `Assets/TPLab/Core`, `Assets/Plugins/CsvHelper` | `Runtime/`, `Runtime/ThirdParty/CsvHelper/` | UniTask 2.5.11, Addressables 2.9.1; 포함 CsvHelper 33.1.0 DLL |
| `com.tplab.input` | `Assets/TPLab/Input/Runtime` | `Runtime/` | 같은 버전 Core, UniTask 2.5.11, Input System 1.19.0 |
| `com.tplab.editor` | `Assets/TPLab/Editor` | `Editor/` | 같은 버전 Core, UniTask 2.5.11, Addressables 2.9.1, Newtonsoft.Json 3.2.2 |

package ID는 새 배포 명세의 후보 식별자다. 첫 구현에서 중복·명명 검사를 통과한 뒤 고정한다. C# namespace·assembly 이름 `TPLab.Core`, `TPLab.Core.Input`, `TPLab.Core.Editor`는 유지한다. Input·Editor 설치는 선택이며 Core에 입력·uGUI·URP를 강제하지 않는다. 개발용 Connector·IDE·Unity Test Framework를 소비 runtime dependency에 추가하지 않는다.

개발 예제는 `Assets/TPLab/Samples/Core`와 `Assets/TPLab/Samples/Input`으로 분류한다. Core 기능만 필요한 예제는 Core, Input을 함께 사용하는 예제는 Input에 둔다. 실제 의존성으로 분류하며 폴더 이름만 바꿔 Core-only 지원을 주장하지 않는다. 씬 전환 예제는 Core·Input·uGUI 2.0.0을 사용하므로 `Assets/TPLab/Samples/Input/SceneTransitions`에서 `com.tplab.input`의 선택 sample로 제공한다. `Assets/TPLab/Samples/Core/CorePooling`은 Core만 사용한다. P2에서 두 sample의 실제 import/실행과 기존 GUID 보존을 확인했다.

package sample 폴더·manifest 설정은 Unity 6000.3의 `Samples~`와 `samples` 규격을 사용한다. Git URL과 tarball 양쪽에서 실제 `Sample.Import`와 Assets의 import 결과를 확인했다. 두 설치 방식의 상대 sample 경로·구성이 같아야 한다. 샘플 import만으로 사용자 Input Handling·Build Settings·Addressables·시작 씬을 자동 변경하지 않는다. 데이터 테이블 템플릿은 Editor package의 선택적 sample 후보다. 기존 테스트/fixture·Validation 스크립트는 기본 소비 패키지에서 제외하고 검증 checkout이 소유한다. package 자체의 테스트 구성이 필요해지면 별도 opt-in 경계를 검토한다.

[Unity package layout](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-layout.html)과 [Samples 규격](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-samples.html)을 따른다. `Runtime`·`Editor`의 기존 파일/폴더 `.meta`는 매핑 대상마다 한 번만 복사하며 GUID를 보존한다. 패키지 root·새 생성 문서에는 기존 부모 GUID를 중복 전파하지 않는다. 소스 복사본과 같은 GUID/assembly의 패키지를 한 소비 프로젝트에 동시에 설치하지 않는다.

## 개발 소스와 생성 공간

개발 원본은 계속 `Assets/TPLab`에 둔다. **`upm/`은 Git URL로 제공할 package 사본을 버전 관리하는 공간**이다. 원본·문서·라이선스에서 생성한 사본을 검토한 뒤 명시적으로 커밋하며 직접 수정하지 않는다. Unity 개발 프로젝트의 Assets/Packages 밖에 있어 개발 assembly를 중복 import하지 않는다. release 후보에는 원본과 `upm/`의 파일 매핑·내용·GUID·버전 일치 검사를 수행한다.

저장소 루트 하위 **`tplab/`은 빌드 요청 시 생성하는 공간**이다. 활성 개발 checkout과 구별되는 배포 worktree에서 생성하고, P1 구현 때 root 한정 `/tplab/` ignore 규칙을 추가한다. `upm/`은 ignore하지 않는다. P1은 `/tplab/` ignore와 생성 도구를 구현했다. `upm/` 사본은 준비 output을 검토한 뒤 별도 커밋하며 생성기가 Git을 변경하지 않는다. 실제 생성·사본 일치 결과는 [P1 증거](validation/distribution-packaging/README.md)를 따른다.

```text
<배포-worktree>/
  Assets/TPLab/                  # Git에서 추적하는 개발 원본
  tools/                        # 반복 실행 도구
  upm/                          # Git에서 추적하는 배포 사본, P1에서 생성
    com.tplab.core/              # package.json + Runtime + 문서·고지
    com.tplab.input/
    com.tplab.editor/
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

첫 배포는 Package Manager의 **Install package from Git URL** 또는 Release에서 받은 `.tgz`의 **Install package from tarball**로 설치한다. Core를 먼저, 선택한 Input·Editor를 그 다음에 설치한다. 사용자 프로젝트의 package 원본은 유지하고 필요한 manifest 항목만 합친다. [Unity tarball 설치](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-ui-tarball.html)를 기준으로 검증한다.

UniTask의 현재 원본은 Git URL이므로 소비 프로젝트 `Packages/manifest.json`에 직접 고정한다. UPM은 package 간 Git URL dependency를 지원하지 않는다. TPLab package manifest에는 버전 계약을 기록하고 **소비 프로젝트가 실제 공급원을 명시**하도록 설치 안내와 검증 도구가 확인한다. TPLab Input/Editor의 Core dependency 역시 registry가 공급한다고 가정하지 않고 소비 manifest에 Core의 Git URL 또는 `.tgz` 공급원을 먼저 지정한다. [Unity Git dependency 제약](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-git.html).

아래는 **소비 프로젝트 설정 예시**다. P2에서 실제 provider resolve/실행을 확인했으며 정식 Release 첨부물은 P4 이후 제공한다. 각 PC가 다운로드한 실제 파일 위치로 바꾼다. 설치 대상 프로젝트 root 아래 `Vendor/TPLab/`에 두는 예를 사용하며 경로는 `Packages/manifest.json` 기준이다. URL에 계정 비밀값을 넣지 않는다.

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

Git 설치는 실제 tag에 포함된 `upm/`을 사용한다. 다음 URL은 **P4 이후 정식 설치 예시**다. `upm/`은 존재하며 후보 SHA 설치를 검증했다. `v0.0.1`은 아직 발행하지 않았으므로 현재 tag URL의 설치 성공을 주장하지 않는다.

```text
https://github.com/KangSungKyu/TPLab.git?path=/upm/com.tplab.core#v0.0.1
https://github.com/KangSungKyu/TPLab.git?path=/upm/com.tplab.input#v0.0.1
https://github.com/KangSungKyu/TPLab.git?path=/upm/com.tplab.editor#v0.0.1
```

소비 project manifest에서 해당 TPLab 공급원을 위 URL로 지정하고 UniTask 및 선택한 Unity package 공급원도 앞의 계약대로 명시한다. 후보 검증에는 `#<40자리-후보-SHA>`를 사용하고 Release gate에서 태그가 같은 SHA를 가리키는지와 tag URL의 실제 설치를 확인한다. `?path` 뒤에 `#revision`을 쓰고 해당 revision의 subfolder에 package.json이 있어야 한다. 같은 저장소의 복수 package를 설치할 때도 각 공급원을 따로 지정한다. [Unity Git URL 규격](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-git.html).

생성 전용 `tplab/`은 Git tag에 들어가지 않으므로 Git UPM path로 사용하지 않는다. 개발 clone과 소비 UPM 설치를 구분한다. 첫 배포에 registry 서버·Jenkins·GitHub Actions를 추가하지 않는다.

## dev-build track 운영

브랜치는 commit 흐름을, worktree는 각 PC의 별도 checkout을 담당한다. 같은 PC의 worktree는 Git object/ref를 공유하지만 다른 PC는 자체 clone과 worktree를 만든다. worktree 폴더를 복사하여 이동식 환경으로 제공하지 않는다. source SHA·Unity 버전·의존성 원본·실행 도구를 고정해 어느 PC에서든 같은 절차로 생성한다. Unity 설치·활성화·Windows Mono 모듈과 원격 package 접근 권한은 각 PC가 준비한다. 원격 의존성을 모두 내려받은 뒤의 동작과 완전 오프라인 빌드는 구분한다. [Git worktree 공식 안내](https://git-scm.com/docs/git-worktree).

- P0 최초 명세는 `codex/distribution-design`에서 작성해 main에 반영·작업 브랜치 정리를 완료했다. 이번 승인 내용 보완은 `codex/distribution-refinement`에서 문서 검증 후 기존 승인에 따라 반영한다. 문서 변경에 사용자가 Unity로 수락할 새 동작은 없다.
- 구현 시작 시 실제 최신 main·dirty·원격을 확인하고 `codex/dev-build-v0.0.1` track을 만든다. 그 최신 통합 tip에서 `codex/dev-build-v0.0.1-p1-packaging`, `-p2-consumer`, `-p3-candidate`를 필요한 시점에 생성한다. 이번 P0에서는 이 브랜치들과 worktree를 만들지 않는다.
- Phase는 track으로 순차 통합한다. 공용 계약·source mapping·Unity 실행은 부모가 소유한다. 하위 에이전트는 기존 승인 범위에서 독립 조사/문서 검증이 필요할 때만 사용하며 같은 파일/Editor를 동시에 조작하지 않는다.
- `tplab/` 생성은 읽은 checkout의 HEAD가 명시한 40자리 source SHA와 일치하고 추적 파일이 clean일 때만 허용한다. dev-build 단계의 미커밋 소스는 빌드에 섞지 않는다. 다른 작업의 dirty를 stash/reset/clean하지 않고 격리한 checkout에서 작업한다.
- 배포 사본은 clean 원본에서 별도 생성·검토해 `upm/`으로 명시적 커밋한다. 사본 생성 도구가 Git add/commit을 자동 수행하지 않는다. 사본 내부에 사본을 포함한 미래 커밋 SHA를 기록하지 않아 해시 자기 참조를 피한다. 최종 후보 SHA는 원본과 검토된 사본을 모두 포함하고, 같은 후보에서 사본 재생성 일치와 tarball payload 일치를 확인한다. candidate SHA는 package 밖의 distribution manifest·검증 evidence에 기록한다.
- 후보 생성은 track의 **정확한 커밋**을 detached build worktree로 checkout해 수행한다. 생성 산출물 외에 보존할 미추적 작업이 발견되면 중단한다. worktree는 경로·source SHA·역할·상태를 기록하고 소유한 것만 도구의 관리 절차로 정리한다.
- main 통합은 필요한 실제 자동 검사와 남은 사용자 확인을 완료한 뒤 fast-forward 또는 일반 merge로 한다. 최신 main을 반영하면 결과 커밋 기준으로 다시 빌드·검증한다. 이전 후보 산출물을 새 commit의 통과로 재사용하지 않는다.
- 최종 태그는 **검증한 source commit**을 가리킨다. 이후 마감 문서 commit이 추가됐다고 태그를 이동하지 않는다. source commit이 main 이력에 보존됐는지 확인하고 package version·distribution manifest·검증 보고·Release source를 일치시킨다.
- Phase/track 브랜치 정리는 main push·원본 commit 보존·해당 branch tip/원격 일치·worktree 미사용을 확인한 뒤 기존 조건부 삭제 규칙을 따른다. 활성 build worktree나 후보 결과가 필요한 동안 먼저 삭제하지 않는다.

## 최소 실행 도구와 기록

후속 P1은 Python 표준 라이브러리를 우선 사용한다. git checkout/인증은 기존 Git, package 소비 build는 기존 Unity batch 경로를 사용한다. 자동화 도구가 저장소 생성·인증 설정·네트워크 정보 출력·승인되지 않은 publish를 함께 수행하지 않는다. 한 명령의 실패 exit가 후속 build/merge/publish를 막아야 한다.

후보 CLI는 아래와 같다. **[tools/build_distribution.py](../tools/build_distribution.py)는 P1에서 구현했다.** version/run ID/path/source를 검증하고 파일 읽기·패키지 생성·hash/manifest 생성만 담당한다. Git 브랜치·merge·publish는 해당 Git 단계가 담당한다.

```text
python tools/build_distribution.py --source <절대-clean-checkout>
    --revision <40자리-HEAD> --version 0.0.1 --run-id <고유-ID>
    --output <해당-checkout>/tplab/<고유-ID> [--prepare]
```

`--prepare`는 최초 사본 또는 갱신할 사본을 새 output에 준비한다. 기본 mode는 커밋된 `upm/`과 새로 생성한 package의 파일 집합·bytes 일치를 요구한다. prepare output의 packages를 검토해 `upm/`에 명시적으로 반영·커밋한 뒤, 새 exact SHA와 새 run ID로 기본 mode를 실행한다. 사본을 바꾸면 다시 생성·검증한다. 어느 mode도 설치 gate를 완료시키지 않으며 현재 `publishable: false`를 유지한다. 문서/API·DLL·meta는 Git에 저장된 bytes를 기준으로 읽는다. Python/zlib 버전은 manifest에 기록하고 해시 재현 근거에서 확인한다.

`distribution-manifest.json`은 schemaVersion, sourceRevision, version, package별 ID/버전/filename/SHA256, source-to-package mapping hash, 의존성 계약, 필수 검증/정책 상태를 기록한다. 실행 PC/시각/원래 Editor PID·검증 consumer PID·실제 test count·exit와 로그 경로는 별도 검증 evidence에 둔다. MIT/제3자 고지·필수 설치/build 검증을 충족하지 못한 개발 검증본은 `publishable: false`로 표시한다. 라이선스 결정만으로 artifact 설치 검증을 완료로 표시하지 않는다. 새 파일과 증거의 상세 schema는 P1에서 실제 명령 구현과 함께 확정한다.

재사용 대상은 [도구 안내](../tools/README.md)와 기존 도구다. `run_core_consumer.py`는 기본 source-copy mode와 별도의 tarball/Git 설치 mode를 제공한다. P2에서 빈 소비 프로젝트의 실제 UPM resolve·compile·최소 실행을 확인했다. 실제 artifact mode에는 Assets의 Core/Input/Editor 사본을 넣지 않는다. 배포 증거 경로·tamper·payload·symlink 거부 계약도 테스트했다. 기존 원본 Editor 검증은 기존 Editor에서, 독립 소비 검증은 명시된 별도 batch consumer에서 실행한다. 두 검증을 서로 대체하지 않는다.

## 구현 Phase와 완료 조건

| Phase | 범위 | 완료 조건 | 현재 |
|---|---|---|---|
| P0 규격·운영 | 이 문서, 입구/지침/회고 연결 | 현재 소스/assembly/의존성 대조, 상대 링크·범위·공백·보호 검사 | 완료 / main 반영·public 확인 |
| P1 패키징 | 원본→검토할 `upm/` 사본, clean 후보 SHA→3개 `.tgz`, 문서/라이선스 projection, hash/manifest | 최소 Red/Green: dirty·잘못된 SHA·경로 탈출·기존 output·symlink 거부; 같은 입력의 archive hash 일치; 원본/사본/tarball payload·버전/`.meta`/DLL 일치 | 구현·계약 테스트18/18 완료; 실제3 archive·사본171 files·재현 검증 완료([증거](validation/distribution-packaging/README.md)) |
| P2 실제 설치 | Git URL·tarball 소비 mode, Core/Input sample 분류·import 경로 수정, Editor importer 수명 | 각 설치 방식의 Core만/Input/Editor/전체+Sample resolve·compile·최소 실행, negative 경로, 원본 보호 | 8/8 Git/tarball 실제 설치·compile·최소 실행 완료([P2 증거](validation/distribution-consumer/README.md)); 개발 경로 fixture·원본 보호 확인 |
| P3 배포 후보 | commit 고정, 회귀·Windows Mono sample/consumer·문서/정책 gate | source/산출물/결과 일치, 실제 전체 결과 nonzero, 필요한 사용자 확인 완료 | 완료 / 자동 검증·[사용자 확인](DISTRIBUTION_ACCEPTANCE.md) 완료 |
| P4 첫 Release | main 통합·`v0.0.1`·public Release와 검증된 첨부물 | 정책/필수 gate 충족, source tag·SHA256·버전 일치·tag URL 설치·다운로드한 실제 첨부물 검증, 브랜치 정리 | 정식 공개·tag URL·첨부물 검증 완료 / 브랜치·장치 마감 후속 |

현재 코어의 [PlayMode 수락](SCENE_LOADING_ACCEPTANCE.md)은 선행 기록이다. 패키지 설치/sample import/새 Editor workflow의 사용자 확인이 필요하면 P3 마지막에 모아 실제 실행 방법·기대 결과를 전달한다. 응답이 바로 이어지지 않으면 track 결과를 보존하고 main 통합·Release를 기다린다. 사용자 확인 항목이 없는 변경은 기존 자동 검증 후 병합 정책을 따른다. 시간 경과를 승인으로 보지 않는다.

P2의 필수 수정/관찰은 다음과 같다.

1. [SceneTransitionSamplePaths](../Assets/TPLab/Samples/Input/SceneTransitions/Runtime/SceneTransitionSamplePaths.cs)의 고정 `Assets/TPLab/Samples/Input/SceneTransitions` 경로와 [sample builder](../Assets/TPLab/Samples/Input/SceneTransitions/Editor/SceneTransitionSampleBuilder.cs)의 marker/asset 경로가 새 개발 경로 `Assets/TPLab/Samples/Input/SceneTransitions`와 UPM의 sample import 위치를 처리해야 한다. 이동하는 기존 asset/script의 `.meta`·GUID·scene 참조는 보존한다. 수정은 sample 계층에 한정하고 실제 source 위치와 import 위치에서 정상/중단/원본 복원을 검증한다.
2. [importer 설정](../Assets/TPLab/Editor/DataTables/DataTableImportSettings.cs)의 `Assets/Editor/TPLab/setting.asset`은 소비 프로젝트 소유로 유지한다. [생성 보호](../Assets/TPLab/Editor/DataTables/DataTableGeneratedFiles.cs)는 package 읽기 전용 영역을 허용하지 않고 프로젝트의 Assets 생성 경로를 검증한다. 설정 없음→자동 작업 없음, validator 실패→이전 생성 소스 보존을 artifact 설치 후 확인한다.
3. 배포 README와 사람/AI API는 포함 모듈에 맞춰 선별하고 실제 package 안에서 해결되는 상대 링크로 source/API 경로를 변환한다. 다른 package의 참조는 존재가 보장되는 공식 문서/Release 위치로 연결하거나 설치 안내에서 설명한다. 개발 저장소의 `Assets/TPLab` 링크·Temp·개인 경로를 배포본에 그대로 두지 않는다. 기존 근거는 [문서 지침](DOCUMENTATION_GUIDE.md)이다.
4. 현재 Core PlayMode 테스트 assembly는 sample·Input·uGUI를 함께 참조한다. Core-only artifact 소비 compile/실행과 전체 개발 회귀를 별도로 수행하고, 전체 테스트 폴더를 Core package에 복사해 Input을 우회 강제하지 않는다.

## 정책·Release gate와 실패 처리

사용자는 외부 제공을 허용하고 추가 제한 없이 참조 의존성의 최소 조건을 지키도록 요청했다. TPLab 자체 구현에는 MIT를 적용한다. MIT의 copyright/permission 원문을 보존하고, 변경 내용과 원본 공급원이 다른 제3자에 TPLab MIT를 덮어씌우지 않는다. CsvHelper DLL의 `MS-PL OR Apache-2.0` 선택권·원문 고지·버전·hash를 보존하며 UniTask 등 외부 package의 원문/버전을 안내한다. Unity package는 공식 원본 설치를 유지한다. package manifest의 `license: MIT`는 TPLab 자체 구현의 조건이며 포함된 제3자는 Third Party Notices의 원문 조건을 따른다. 라이선스/고지·필수 검증 누락 시 최종 Release를 발행하지 않는다.

Unity 버전 기준은 정확히 `6000.3.18f1`, 첫 실제 Player 검증 범위는 Windows Mono다. 다른 Unity/플랫폼/IL2CPP 지원은 별도 증거 전에는 주장하지 않는다. 동일 PC의 재현 성공과 실제 다른 PC 실행 성공도 구분한다. `packages-lock.json`은 소비 검증/개발의 resolved 근거이며 사용자 manifest 전체를 덮어쓰는 배포물로 사용하지 않는다.

필수 test/build/설치 실패, source SHA 불일치, 0건·stale·누락 결과, hash/정책 미확정은 **실패/대기**로 기록하고 다음 gate를 진행하지 않는다. 의도한 run의 결과만 읽으며 불명확한 테스트를 재실행해 최초 실패를 지우지 않는다. package를 검증한 뒤 version/source/내용을 수정하면 다시 생성·검증한다.

GitHub Release는 검증한 tag 기반 Git 설치 URL, source tag, 3개 `.tgz`, `SHA256SUMS.txt`, 설치/검증 안내와 변경 내역을 연결한다. 태그/Release가 기존에 있으면 자동 교체·삭제하지 않는다. 필요한 첨부물을 준비한 draft 상태와 실제 published 상태를 구분한다. 이번 사용자가 승인한 public 저장소의 Release는 외부 수신자가 다운로드하는 공개 배포다. 저장소 공개와 `v0.0.1` 패키지 발행을 별도 상태로 보고한다. [GitHub Release 절차](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

CI는 현재 미구성이다. [exact commit gate](../tools/check_github_ci.py)는 main 보호·check/workflow 유무를 확인하지만 Unity test/build 실행을 대신하지 않는다. 실패/대기 중의 자동 재시도·장치 종료·예약 실행은 이번 배포 규격에 추가하지 않는다. 각 Phase 결과·source SHA·증거·미완료는 회고/track 상태에 기록하고 문서 검증 완료를 배포 검증 완료로 표시하지 않는다.

## 다음 버전 범위

사용자는 다음 버전에 현재 Core 내부 기능의 외부 의존성 분리 검토와 `GameUISystem`을 요청했다. 첫 `0.0.1`에는 현재 Core 의존성 계약을 유지한다. 분리 판단과 UI 설계의 선행 조건은 [후속 계획](CORE_PLAN.md#다음-버전-계획-2026-10-07)이 소유하며 차기 버전 번호·package/API 변경은 아직 정하지 않았다.

## P1 작업 기록 (2026-10-07)

기준 main `af9958ba402be2bc7f31d453afb2b14294a47ac8`에서 track `codex/dev-build-v0.0.1`과 phase `codex/dev-build-v0.0.1-p1-packaging`을 생성했다. 관리형 worktree 도구가 이전 MyLab 경로에서 Git을 찾지 못해 Git worktree로 직접 격리했다. 개발 checkout의 사용자 씬·설정 변경은 배포 입력에 포함하지 않는다. P1은 도구·패키지 사본·문서만 작업하며 Runtime/Editor·sample 이동과 UPM 실제 설치는 수행하지 않는다. P1은 검증 뒤 track으로 통합하며 main/tag/Release gate는 P2/P3 이후다.

| Agent | Model / effort | 역할·허용 경로 | 상태 |
|---|---|---|---|
| `/root/distribution_docs_review` | gpt-6-luna / low | P1 checkout의 기존 API와 builder/test 읽기 전용 리뷰; Git·Unity·파일 변경 금지 | 완료: 링크 projection·코드 경계 검토, 실제 설치 증거와 구분 |

부모가 도구 구현·테스트 실행·사본 리뷰·Git 통합을 담당한다. [회고](retrospectives/2026-10-07-17-distribution-packaging.md)에 검증·남은 Phase를 기록한다.

P1 완료: 사본 포함 `ec1f786d248207a8c9245b6d5263f95a1985e093`에서 기본 mode와 두 번 생성한 archive3개의 bytes/manifest 일치를 확인했다. source/사본·문서/GUID/license·원본 보호는 P1 증거를 따른다. P1을 track으로 반영하고 다음 phase는 P2 실제 설치다. source/tag용 사본은 준비됐지만 `v0.0.1` tag·검증된 설치 안내/Release는 아직 제공하지 않는다.

## P2 작업 기록 (2026-10-07)

Track `23e2764993e46d3fcd18e8d46d8029ac1863f1df`에서 `codex/dev-build-v0.0.1-p2-consumer`를 생성했다. 원본 Editor와 사용자 씬/설정을 보존하며 별도 batch consumer에서 설치를 검증한다.

| Agent | Model / effort | 역할·허용 경로 | 상태 |
|---|---|---|---|
| `/root/distribution_samples` | gpt-6.1-sol / medium | `Assets/TPLab/Samples` 이동·import 경로와 Core 예제, 관련 경로 테스트; Git/Unity 금지 | 완료: 25 GUID 보존·경로/예제 구현, 부모 실제 검증 |
| `/root/distribution_editor_probe` | gpt-6.1-sol / medium | `tools/core-consumer/templates/EditorProbe.cs` 설치 후 importer 검증 harness; Git/Unity 금지 | 완료: 두 launch probe 구현, 부모 실제 검증 |

부모는 패키징/소비 Python 도구·Unity 실행·결과 리뷰·문서·Git을 소유한다.

P2 완료 후보: `5f2e08d8bacb2320ca0832120f194c326869ce40`. Git URL/실제 tarball8/8, consumer Editor18+Player8+sample Player4(48 checks), 개발 경로 fixture4process/두 모드24 checks, 33 Python methods32pass/1OS skip, Unity path12/12 Green. [P2 증거](validation/distribution-consumer/README.md)와 [회고18](retrospectives/2026-10-07-18-distribution-consumer.md)가 선행 상태를 소유한다. 부모가 source/harness/payload를 리뷰하고 phase를 track으로 통합·푸시하며 main/tag/Release와 phase 삭제는 진행하지 않는다. P3는 이 후보의 코드/산출물 해시와 후속 변경 범위를 확인한 뒤 fixed release candidate 회귀·최종 확인을 수행한다.

## P3 작업 기록 (2026-10-08)

Track5ffe2a9에서 P3 candidate branch를 이어 진행했다. 최종 검증 source `de879b9396ddae0523bd3ab86939b679b383dd92`은 원본·검토된 upm 사본을 포함한다. 정확한 후보에서 Edit286/286·Play253/253, Git/tarball8/8(30process·sample48checks), archive3개 bytes 재현, Python32pass/1OS skip, 원본 main·사용자파일6개 보호를 확인했다. [P3 증거](validation/distribution-candidate/README.md), [최종 확인](DISTRIBUTION_ACCEPTANCE.md), [회고](retrospectives/2026-10-08-01-distribution-candidate.md)를 따른다. 최초 실패는 별도 기록에 보존하며 product Input runtime은 변경하지 않았다.

| Agent | Model / effort | 역할·허용 경로 | 상태 |
|---|---|---|---|
| `/root/distribution_editor_probe` | gpt-6.1-sol / medium | Editor파일교체·긴경로·testfixture/source/upm diff 읽기 전용 리뷰; Git·Unity·파일변경·재위임 금지 | 완료: 최소 변경·cleanup·문서 계약 대조, 실제 실행은 부모 담당 |

P3 phase를 track에 fast-forward 통합·push하고 사용자 확인을 기다린다. tag용 frozen source는 `de879b9396ddae0523bd3ab86939b679b383dd92`이며 후속 증거 마감 commit으로 tag를 이동하지 않는다. main은 원래af9958에 그대로 있다. P4 발행·tagURL/실제첨부물 다운로드·브랜치정리·Unity/PC종료는 아직 진행하지 않았다.

## P4 작업 기록 (2026-10-08)

사용자가 DISTRIBUTION_ACCEPTANCE.md 내용을 실행·확인했다고 명시했다. 현재 원격 main이 원래af9958임을 확인하고 트랙을 fast-forward 병합·push했다. 검증 source de879b9가 main 이력에 포함됨과 v0.0.1 tag peel 일치를 확인했다. 실제 tag URL로 Core/Input/Editor와 두 Samples를 설치해 Editor3·기본Player1·samplePlayer2의6process(24 sample checks)를 통과했다. 공개 Release 첨부물8개를 비인증 다운로드해 원본 bytes와 비교했다. [발행 증거](validation/distribution-release/README.md), [회고](retrospectives/2026-10-08-02-distribution-release.md)를 따른다.

원본 사용자파일6개는 병합 전·후 hash가 같고 기존 Editor의 최신 main import/compile 준비와 C# 오류0을 확인했다. 제품 Console에는 이전 의도된 테스트 예외가 있어 전체 Console 무오류로 표현하지 않는다. 후속 저장은 사용자 요청에 따른 정상 Editor 저장이며 저장으로 생긴 사용자 변경을 배포 commit에 포함하지 않는다. 작업 tip을 main에 보존하고 로컬/원격 tip·worktree 사용 여부를 확인한 뒤 해당4개 브랜치만 정리한다. Unity 정상 종료를 확인한 뒤 PC를 종료하며, 종료 실패 시 오프라인/절전 대안을 따른다.
