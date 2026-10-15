# 가이드 결정 D-I1~D-I8·테스트 교훈·CI 판정 문서 이동

위험 등급: **T0 · 문서만**. 사용자 2026-10-15 A~E 요청을 반영한다. 기준은 main `af084e0d0027c4098fad08c336ccb0753e1c7862`, 브랜치는 `codex/guidelines-decisions-i`다. 소스·테스트·공개 API·assembly·패키지·도구·workflow·UPM 변경은 없다. 원격 main을 fetch하고 로컬/원격 동일·ahead/behind 0/0을 확인한 뒤 새 브랜치를 만들었다. 병합·이력 재작성·브랜치 삭제는 하지 않는다.

## 범위와 결정

| 파일 | 변경 전 → 변경 후 |
|---|---|
| `doc/guidelines/README.md` | 초안·미결정 목록·구형 source → 현행 가이드, D-I1~D-I8 사용자 결정, main source 통일, 남은 유보 범위, 실제 라벨 표·체크리스트 입구 |
| `01-code-conventions.md` | 구형 source/줄 인용 → 현 main·실제 guard 줄, 기존 집계의 당시 source 구분 |
| `02-public-api.md` | 구형 source/AGENTS 인용 → 현 main·실제 XML 지침 줄; 계약 결정 유지 |
| `03-async-lifecycle.md` | 구형 source/줄 인용 → UIContext 주석 이동·root TearDown 보강 이후 실제 위치; 수명 동작 변경 없음 |
| `04-allocation-gc.md` | 구형 source/상대 증거 링크 → 현 main source·고정 SHA 증거 링크; GC 정책 변경 없음 |
| `05-testing.md` | Red 동결·Timeout 제안 → D-I1/D-I2 결정, IMP-04b 전 세부 미확인, 안전망·fixture 현행 교훈 |
| `06-documentation.md` | 머리말·CI 위치 제안 → D-I3 형식 정의/후속 T0·D-I4 이동; 이관 상세와 현재 인용 정렬 |
| `07-validation-evidence.md` | 과거 입력 확보·CI 위치 제안 → D-I5 역사적 기록 유지·D-I4 위치 확정 |
| `08-change-procedure.md` | 14×4 적용/인용 제안 → D-I6/D-I7 결정; 56칸 내용과 공통 필수 유지 |
| `CHECKLIST.md` | 없음 → 한 화면 분량의 기존 결정됨/현행 요약과 항목별 원문 링크 |
| `doc/CI_GATE_PROPOSAL.md` | validation 하위 → doc/ 위치; 자체 상대 링크 한 곳만 보정 |
| `AGENTS.md` | CI 판정 링크 옛 경로 → 새 경로 한 곳; 나머지 bytes 보존 |
| `doc/CI_SETUP_GUIDE.md`, CI validation README, AGENTS_CHANGE, 과거 CI 회고 | 본문·역사적 설명 유지, 이동한 문서의 링크 대상만 수정 |
| `doc/INDEX.md`, 회고 INDEX | CHECKLIST·CI 판정 기준/운영 가이드 관계, 이번 회고 상태+링크 |

D-I1~D-I8은 각각 사용자 결정 2026-10-15를 명시했다. D-I2는 기본값+메서드별 차등 방침만 채택했고 구체 값·적용 범위, 기본값 적용·메서드 우선·ToCoroutine hang 중단 실험은 미확인이다. D-I3는 유형·현재 상태·기준 source·갱신일·계약 링크 형식 정의만 했으며 머리말 일괄 통일은 하지 않았다. 기존 장의 source 표기는 요청 E에 따라 갱신했고 기존 집계 수치를 재측정한 것으로 바꾸지 않았다.

D-I6의 적용 표는 14행·56칸이다. 각 칸의 상태 라벨을 제외한 문자열이 전후 완전히 동일함을 검사했다. 근거 열의 AGENTS 인용만 현재 위치로 바로잡았다. 모든 등급의 사용자 병합 승인·exact-SHA CI·dirty 보존은 유지한다.

## 테스트 교훈의 실제 근거

| 반영 | 직접 읽은 근거 |
|---|---|
| 정상본 통과 → 단일 변이 실패 → 원복 재통과·이름/수·반복 | [UI-A:79–104](2026-10-14-07-uicontext-safetynet-a.md#L79) |
| fixture 정정 후 변이 재실행 | [Guard-A:267–317](2026-10-14-09-guard-safetynet-a.md#L267) |
| InputTestFixture 구성 Setup/finally TearDown·복원 후 LogAssert | [Guard-A:220–228](2026-10-14-09-guard-safetynet-a.md#L220) |
| No log scope 원문·동기 시작의 원인·iterator 마지막 단언 | [Guard-A:67–91](2026-10-14-09-guard-safetynet-a.md#L67), [종료 대기 보강:25–29,66](2026-10-15-05-test-teardown-hardening.md#L25) |
| 신규 제외 기준선·그룹별 단독 포함·오염 격리 | [Guard-A:180–218](2026-10-14-09-guard-safetynet-a.md#L180) |

가이드의 이 증거 인용은 위 main에 고정한 commit SHA permalink로 썼다. 과거 실험 결과를 이번 실행으로 재서술하지 않았다. EventSystem만 사용하는 기존 fixture의 참조 제약은 종료 대기 회고:29에 남아 있으며 이번 PR은 fixture·assembly를 고치지 않는다.

## 이동 전 검색과 링크 보정

검색: `rg -n --hidden --glob '*.md' --glob '*.py' --glob '*.yml' --glob '*.yaml' 'CI_GATE_PROPOSAL|ci-review-p1[\\/]CI_GATE' .`를 generated `.git/Library/Temp/Logs/obj/tplab` 제외로 실행하고, `git grep -n -I CI_GATE_PROPOSAL -- .github CLAUDE.md tools`로 보호 경로를 재확인했다. `.github`·`CLAUDE.md`·`tools` 참조 0이다. 보호 경로 수정은 없다.

| 문서·이동 전 줄 | 보정 대상 | 수 |
|---|---|---:|
| `AGENTS.md:118` | `doc/validation/ci-review-p1/CI_GATE_PROPOSAL.md` → `doc/CI_GATE_PROPOSAL.md` | 1 |
| `doc/CI_SETUP_GUIDE.md:3` | `validation/ci-review-p1/CI_GATE_PROPOSAL.md` → `CI_GATE_PROPOSAL.md` | 1 |
| `doc/guidelines/07-validation-evidence.md:24` | `../validation/ci-review-p1/CI_GATE_PROPOSAL.md` → `../CI_GATE_PROPOSAL.md` | 1 |
| `doc/validation/ci-review-p1/README.md:19` | `CI_GATE_PROPOSAL.md` → `../../CI_GATE_PROPOSAL.md` | 1 |
| `doc/validation/ci-review-p1/fix-p1/reviewer/AGENTS_CHANGE.md:21,25` | 저장소 상대 링크 대상 → `../../../../../doc/CI_GATE_PROPOSAL.md` | 2 |
| `doc/retrospectives/2026-10-12-04-ci-review-automation.md:32` | `../validation/ci-review-p1/CI_GATE_PROPOSAL.md` → `../CI_GATE_PROPOSAL.md` | 1 |
| 이동 파일 자체:27 | `fix-p1/README.md` → `validation/ci-review-p1/fix-p1/README.md` | 1 |

06장·INDEX에는 새 위치의 현재 링크도 추가했다. 과거 회고와 AGENTS_CHANGE의 설명·인용 문장, 기존 inline code의 옛 경로는 그대로다. 링크 대상 역변환 후 원본 bytes와 같음을 검사했다. 이동은 `git mv`로 수행하고 판정 본문은 자체 링크 한 곳 역변환으로 동일함을 확인했다.

## 줄 인용 정정

기존 가이드의 줄 인용 255곳을 작성 당시 source와 현 main 파일의 실제 줄로 대조했다. 같은 원문이 이동한 것은 Git 본문 대응으로 확인하고, 회고 지침처럼 본문이 이관된 것은 현재 절을 직접 읽어 바꿨다. 정정 주요 목록은 다음과 같다(줄은 링크 대상 기준).

| 문서 | 인용 | 전 → 후 |
|---|---|---|
| `01-code-conventions.md` | `AGENTS:14` | 14 → 16 |
| `01-code-conventions.md` | `UIContext:542` | 542 → 546 |
| `01-code-conventions.md` | `대표 guard 선언` | 542 → 546 |
| `02-public-api.md` | `AGENTS:17` | 17 → 19 |
| `03-async-lifecycle.md` | `UIContext.cs:459–465·1224–1294` | 459 → 463 |
| `03-async-lifecycle.md` | `AsyncSceneRootTests.cs:421·523·553` | 421 → 440 |
| `03-async-lifecycle.md` | `AsyncSceneRootTests.cs:485–519` | 485 → 504 |
| `03-async-lifecycle.md` | `UIContext.cs:437–441` | 437 → 441 |
| `03-async-lifecycle.md` | `UIContext.cs:436–443` | 436 → 440 |
| `03-async-lifecycle.md` | `UIContext.cs:473–484·511–526·705–706·752–753·1019–1020` | 473 → 477 |
| `03-async-lifecycle.md` | `UIContext.cs:389–409·1167–1174·1293` | 389 → 393 |
| `03-async-lifecycle.md` | `UIContext.cs:437–440·473–484·511–526` | 437 → 441 |
| `05-testing.md` | `파일` | 133 → 152 |
| `05-testing.md` | `AsyncSceneRootTests.cs:135` | 135 → 154 |
| `05-testing.md` | `AsyncSceneRootTests.cs:137–161` | 137 → 156 |
| `05-testing.md` | `AsyncSceneRootTests.cs:386–394·629–657` | 386 → 405 |
| `05-testing.md` | `AsyncSceneRootTests.cs:629–663` | 629 → 648 |
| `05-testing.md` | `AsyncSceneRootTests.cs:146–161` | 146 → 165 |
| `06-documentation.md` | `AGENTS:43` | 43 → 48 |
| `06-documentation.md` | `AGENTS:125` | 125 → 118 |
| `07-validation-evidence.md` | `AGENTS:38,39` | 38 → 42 |
| `07-validation-evidence.md` | `AGENTS:125` | 125 → 118 |
| `08-change-procedure.md` | `TDD:35` | 35 → 39 |
| `08-change-procedure.md` | `현행 CI:125` | 125 → 118 |
| `08-change-procedure.md` | `dirty 보호:74` | 74 → 75 |
| `08-change-procedure.md` | `AGENTS:111` | 111 → 104 |
| `08-change-procedure.md` | `AGENTS:35` | 35 → 39 |
| `08-change-procedure.md` | `AGENTS:125` | 125 → 118 |
| `08-change-procedure.md` | `AGENTS:123,129` | 123 → 116 |
| `06-documentation.md` | 회고 규칙 | `[회고 지침](../../AGENTS.md#L53)` → `[회고 지침:56](../../AGENTS.md#L56)` |
| `08-change-procedure.md` | 회고 규칙 | `[AGENTS:51](../../AGENTS.md#L51)` → `[AGENTS:56](../../AGENTS.md#L56)` |
| `08-change-procedure.md` | 회고 규칙 | `[회고 source/commit 구분:53](../../AGENTS.md#L53)` → `[회고 상세](06-documentation.md#단위-작업-회고-상세--2026-10-13-이관)` |

추가 확인: 07장의 임시/증거 지침은 AGENTS:60 → 실제 규칙 61, 이름·경로 보존은70 →71이다. 06의 회고 근거는51/53 →56/회고 상세 절이다. README의 과거 자동 병합 인용은119~121 → 현재112~118과 D-F 대조로 바꿨다. 0.5단계의 당시 INDEX135/Input47/Scene33/UI3와 1.5단계의 당시 CI 상태는 원래 source의 permalink로 구분한다. 문장 내용이 달라져 자동 줄 대응이 되지 않은 validation 증거는 실제 문장을 열어 링크 대상 보정 외 내용·줄이 유지된 것을 확인했다. 확인하지 못해 미룬 줄 번호는 없다.

## 라벨 재계산

아래 순서는 **현행 / 결정됨 / 제안**이다. 코드 fence·LABEL_COUNTS 표 자체는 분모에서 빼고, 항목·문단·표 행의 첫 상태 라벨만 센다. 56개 적용 칸은 별도로 센다.

| 문서 | 변경 전 실제 | 변경 후 실제 |
|---|---|---|
| `01-code-conventions.md` | 14/3/0 | 14/3/0 |
| `02-public-api.md` | 12/7/0 | 12/7/0 |
| `03-async-lifecycle.md` | 14/7/0 | 14/7/0 |
| `04-allocation-gc.md` | 14/4/0 | 14/4/0 |
| `05-testing.md` | 15/2/2 | 19/4/1 |
| `06-documentation.md` | 12/19/2 | 12/21/1 |
| `07-validation-evidence.md` | 12/13/2 | 12/15/0 |
| `08-change-procedure.md` | 8/12/16 | 8/28/0 |
| `CHECKLIST.md` | 신설 | 4/10/0 |
| `README.md` | 21/19/3 | 24/28/2 |

이전 게재 표는 06의 결정됨11(실제19), 07의 결정됨8(실제13)로 이관 추가분을 누락했고 README 현행30에는 집계 표 자체9행이 포함됐다(제외한 실제21). 기존 수치를 그대로 옮기지 않고 실제 본문과 동일한 분모로 정정했다. 적용 칸은 제안56 → 결정됨56이며 내용은 같다. 남은 제안은 두 종류가 각각 해당 장과 README에서 반복되는 4개 항목이다: 05/README의 Timeout 세부 설정, 06/README의 후속 머리말 통일. 새로운 사용자 결정이 필요한 즉시 중단 항목은 없다.

## D 조사: 현재 병합 문구

현재 main AGENTS:119는 빈 줄,120은 “병합 후 작업 브랜치 정리” 제목이다. 해당 요청의119~121은 과거 조사 위치다. 현재 실제 문장은 다음과 같다.

> AGENTS:112: 모든 변경 등급의 main 병합은 사용자의 명시적 승인 뒤에만 진행한다. 2026-10-13 사용자 승인에 따라 2026-10-02의 추가 확인 없는 병합 승인을 대체하며, D-F 변경 절차의 병합 전 사용자 확인을 생략하지 않는다.

> AGENTS:114: 문서 변경처럼 실행 테스트 대상이 없으면 그 이유를 기록하고 diff·공백·변경 범위 등 해당 변경에 필요한 검사를 통과한 뒤 사용자의 명시적 병합 승인을 기다린다. 테스트 0건이나 테스트 미실행을 테스트 통과로 기록하지 않는다.

D-F(08:16)의 모든 등급 사용자 승인·exact-SHA CI·dirty 보존·병합 전 사용자 확인과 충돌하지 않는다. 따라서 대체 문구 제안은 불필요하며 해당 AGENTS 문장은 수정하지 않았다. AGENTS:118의 승인된 링크 target 변경만 수행했다.

## 검증·한계와 다음 단계

환경: Windows PowerShell, Git, Python3.14. 임시 검사기는 `Temp/guidelines-decisions-i-20261015/`의 작업 소유 파일이다. 정식 tools·workflow는 수정하지 않았다. 기존 Temp 검사의 읽기 전용 링크/anchor 로직을 작업 폴더에 복사해 이번 CHECKLIST·회고를 포함시켰다. 소스 줄 anchor와 Markdown heading을 검사하고 고정 SHA 증거 링크는 Git blob·anchor 존재로 별도 확인한다. HTTP 전체 검증은 하지 않는다.

실행 명령: `C:/Python314/python.exe -B Temp/guidelines-decisions-i-20261015/verify_doc_links.py --label before/after`, `C:/Python314/python.exe -B tools/check_pr_rules.py --base af084e0d0027c4098fad08c336ccb0753e1c7862 --head HEAD`, `git diff --cached --check`, 커밋 후 `git diff --check af084e0d0027c4098fad08c336ccb0753e1c7862 HEAD`, `C:/Python314/python.exe -B tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <40자리 tip> --output Temp/guidelines-decisions-i-20261015/ci-final.json`.

변경 전252 Markdown·상대 링크2073·anchor539·오류0. 변경 후 수치와 최종 커밋 규칙/CI 판정은 아래 검사 확정에 기록한다. 고정 SHA permalink58개는 실제 blob·anchor를 검사해 오류0이다. 기존 dirty10개(기존6+Netcode 패키지/신규 prefab 관련4)의 raw SHA256이 동일하다. 시작 시점 전체 추적 파일의 bytes도 보존 대조한다.

보호 파일 AGENTS.md warning은 예상 항목이다. 사용자 C 승인으로 CI 문서의 경로 링크 한 곳만 갱신하며 본문 변경·CI 도구/workflow 변경은 없다. 이 warning을 오류나 사용자 병합 승인으로 해석하지 않는다. PR은 회고 링크와 T0/보호 파일 경고 사유만 담는다.

미실행: Unity 컴파일·EditMode/PlayMode 테스트·제품 Console·Player/IL2CPP·설치/배포·GC 계측·IMP-04b Timeout 실험. 문서만 바꾼 T0이므로 Unity를 실행하지 않았고 실행0을 통과로 기록하지 않는다. main 병합·push·브랜치 삭제·PC 종료는 하지 않는다. 이후 Timeout 세부 실험과 머리말 통일은 후속 승인 범위에서 수행한다. 임시 검사·해시·집계 파일은 PR 검토 중 재현용으로만 보존하고, PR 수락 뒤 진행 중 참조가 없을 때 이번 작업 명시 목록만 정리한다.

## 검사 확정

문서·범위 정적 검사: 변경 후254 Markdown·상대 링크2016·anchor485·오류0(전후 새 오류0), 고정 SHA 증거 링크58개 blob/anchor 오류0. 라벨 표는 별도 재계산과 전부 일치하며 56칸 내용 불변이다. stage된19개 변경 파일은 Markdown뿐이고 AGENTS:118의 링크 대상 한 곳 외 변경0, runtime·기존 테스트·asmdef·tools·workflow·upm 변경0이다. 사용자 dirty10개와 허용 범위 밖 전체 추적 파일 raw hash가 동일하다. `git diff --cached --check` 오류0이다. 기존 사용자 dirty의 공백은 이번 PR 검사·수정 범위에서 제외했다.

커밋 후 `check_pr_rules.py`와 최종 tip exact-SHA Actions는 동일 base로 확인한다. 결과를 테스트 통과와 혼동하지 않는다. [이 작업 브랜치의 Actions](https://github.com/KangSungKyu/TPLab/actions?query=branch%3Acodex%2Fguidelines-decisions-i)에서 해당 SHA를 확인할 수 있으며 판정은 최종 완료 보고와 구분한다. PR은 open으로 유지하고 사용자 명시적 병합 승인을 기다린다.

문서 source commit은 `50cfbbbb7f1e5e72cfb7fb3c4284bfbacb210b12`다. 해당 변경19파일의 `check_pr_rules`는 오류0·예상 보호 파일 경고1(AGENTS 링크 한 곳), 공백 검사 오류0이다. [source Actions](https://github.com/KangSungKyu/TPLab/actions/runs/38697155607)는 completed/success였고 checks/statuses/runs/workflows 전 페이지 조회가 완전했으며 SHA가 일치했다. 이 검사 요약만 후속 commit으로 추가하고, 내용이 포함된 최종 tip도 같은 exact-SHA 도구로 재확인해 PR #32와 완료 보고에서 제시한다. 새 source 변경·테스트 실행·main 병합은 없다.
