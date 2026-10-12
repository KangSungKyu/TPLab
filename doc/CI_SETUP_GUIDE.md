# TPLab GitHub 리뷰 자동화 도입 가이드 (1단계)

**상태**: 초안. 아직 저장소에 반영되지 않았다. 이 문서의 파일은 모두 `ci-draft/`의 초안이며, 반영은 Codex가 작업 브랜치에서 하고 병합은 사용자의 최종 확인 후에 한다.

## 1. 목표와 범위

- **목표**: PR 단위로 (a) 기계가 확인할 수 있는 규칙을 자동 검사하고 (b) 필요할 때 `@claude`로 독립 리뷰를 받는다. 승인은 지금처럼 사용자가 한다.
- **1단계에 없는 것**: Unity 테스트 실행(GameCI), 브랜치 보호, 필수 검사 지정. 테스트 실행 증거는 지금처럼 Codex의 로컬 실행 결과(`doc/validation/…`)를 쓴다.
- **참고용이다.** 1단계의 검사 결과는 병합을 막지 않는다. 실패해도 사람이 판단한다.

## 2. 파일 구성

| 파일 | 역할 | 시크릿 | 권한 |
|---|---|---|---|
| `.github/workflows/rule-check.yml` | PR 열림·갱신 때 `tools/check_pr_rules.py` 실행 | 없음 | `contents: read` |
| `tools/check_pr_rules.py`, `tools/test_check_pr_rules.py` | diff 기반 규칙 검사와 단위 테스트(Unity 불필요) | - | - |
| `.github/workflows/claude-review.yml` | PR 댓글에 `@claude`가 있을 때만 리뷰 | `CLAUDE_CODE_OAUTH_TOKEN` | `contents: read`, `pull-requests/issues: write` |
| `CLAUDE.md` | 리뷰어 역할·검토 항목·보고 형식 | - | - |
| `.github/pull_request_template.md` | PR 본문에 완료 보고 형식 강제 | - | - |

### 규칙 검사가 보는 것 (`check_pr_rules.py`)

| 수준 | 내용 |
|---|---|
| 오류 | 테스트 파일에 `[Ignore]`, `[Explicit]`, `Assert.Ignore/Inconclusive` 추가 / 테스트 수 순감소(근사치) / 새 `.cs`에 `.meta` 없음 / 삭제한 `.cs`의 `.meta`가 남음 |
| 경고 | 테스트에 `Thread.Sleep`, `Task/UniTask.Delay` 추가 / 단언 수 순감소 / `using System.Linq` 추가 / `.github/`, `CLAUDE.md`, `AGENTS.md`, 검사 스크립트 변경 |

한계: 정규식 기반 근사치다. 테스트 수와 단언 수는 정확한 값이 아니라 **증감 비교용**이다. 이미 있는 항목(예: `DataTableImporterTests.cs`의 `Thread.Sleep(75)`, `Assert.Ignore("Windows path boundary…")`)은 새로 추가될 때만 걸린다. `.meta` 검사는 기준 커밋에 `.meta`가 하나라도 있는 폴더(`Assets`, `upm`)에만 적용한다. Red 테스트 파일이 수정 단계에서 바뀌었는지는 커밋 규칙이 없어 자동화하지 않았다(리뷰어가 본다).

## 3. 먼저 알아야 할 것: `tools/check_github_ci.py`와의 충돌

Codex가 병합 전에 쓰는 `tools/check_github_ci.py`는 **main이 보호돼 있거나 워크플로·체크가 하나라도 있으면** "통합 전에 사람이 확인하라"며 중단한다(`ciConfigured`). 1단계 워크플로를 병합하는 순간부터 이 게이트가 항상 걸려 Codex의 자동 병합이 멈춘다.

그래서 반영 순서에 **스크립트 수정을 포함**해야 한다. 수정 방향은 사용자가 정한다.

- 권장: "워크플로가 있다"는 것만으로는 중단하지 않고, **병합할 SHA의 체크 중 실패·진행 중인 것이 있거나 main이 보호돼 있으면** 중단한다. 보고에는 체크 목록을 그대로 남긴다.
- 이 변경은 병합 안전장치의 의미를 바꾸므로 Codex가 임의로 하지 않고, 변경안과 테스트를 PR로 올려 사용자 확인을 받는다. `AGENTS.md`의 "검증 후 병합" 문단(117~121행)에 CI 체크 해석 문구를 함께 갱신한다.

## 4. 반영 순서

**A. Codex (작업 브랜치에서, 사용자 확인 전 병합 금지)**
1. 초안 파일을 저장소에 반영하고 `doc/INDEX.md`와 `AGENTS.md`의 관련 문서 안내를 갱신한다.
2. `tools` 폴더에서 `python -m unittest test_check_pr_rules`를 실행해 결과를 보고한다(스크립트를 같은 폴더에서 import한다).
3. `check_github_ci.py` 변경안(3절)을 별도 커밋으로 제안하고 테스트를 추가한다.
4. PR을 만든다. 이 PR 자체에 `rule-check`가 돈다. 결과를 PR 본문에 옮긴다.

**B. 사용자**
1. GitHub에서 [Claude GitHub App](https://github.com/apps/claude)을 TPLab 저장소에 설치한다. 이 앱은 Contents, Workflows, Actions 쓰기 등 넓은 권한을 한 번에 요청한다. 설치 전에 목록을 읽고 판단한다.
2. 로컬에서 `claude setup-token`을 실행해 토큰을 만들고, 저장소 Settings → Secrets and variables → Actions에 `CLAUDE_CODE_OAUTH_TOKEN`으로 등록한다. **토큰은 채팅이나 문서에 붙여 넣지 않는다.**
3. 구독 토큰을 쓰면 CI 사용량이 사용자의 구독 한도에 합산된다. `--max-turns 20`과 `timeout-minutes: 20`으로 한 번의 상한을 둔다.
4. A의 PR을 검토하고 승인하면 Codex가 병합한다.

## 5. 첫 실행 확인 (병합 후, 이 순서로)

| # | 방법 | 기대 결과 | 실패 시 |
|---|---|---|---|
| S1 | 테스트용 브랜치에서 테스트 파일에 `[Ignore]` 한 줄 추가한 PR을 연다(병합하지 않고 닫는다) | `PR 규칙 검사`가 실패하고 오류에 해당 줄이 나온다 | 워크플로 경로·`fetch-depth`·SHA 전달 확인 |
| S2 | 같은 방식으로 `.meta` 없는 `.cs`를 추가한다 | `.meta` 오류가 나온다 | `META_ROOTS`와 기준 커밋의 `.meta` 존재 확인 |
| S3 | S1의 PR에 `@claude IMP-NN 검토해줘`라고 댓글을 단다 | Claude가 `CLAUDE.md` 형식으로 댓글을 단다 | App 설치, 시크릿 이름, 권한 확인 |
| S4 | S3 실행 뒤 PR 커밋 목록과 브랜치를 본다 | **Claude의 로컬 파일 생성·커밋도 없다.** 시도 시 로그에 거부로 남는다. PR·브랜치에도 새 커밋·푸시가 없다 | 6절의 쓰기 차단 항목을 우선 해결. 해결 전에는 `@claude` 사용 중단 |
| S5 | Claude 사용량 화면에서 한 번 실행의 사용량을 본다 | 예상 범위 안이다 | `--max-turns`를 낮춘다 |

## 6. 아직 확인하지 못한 것

- 기존 `--disallowedTools`만으로는 로컬 파일 생성·커밋을 막지 못했다는 S4 결과가 보고됐다(로컬 커밋 `4cea32e`). 변경안은 `settings.permissions.deny`와 `persist-credentials: false`를 사용한다. 새 설정의 실제 쓰기 차단은 **미확인**이며, 기본 브랜치 반영 후 PR #2에서 S4를 재검증한다.
- 1차 방어선은 `contents: read` 토큰, 도구 차단(`settings.permissions.deny`)은 2차이며 S4 재검증 결과를 기록한다. 이 설명은 **해당 토큰으로 수행하는 원격 콘텐츠 쓰기**의 범위다. 토큰 권한은 로컬 파일 생성·커밋을 막지 않으며, 별도 GitHub App/설치 토큰의 권한도 자동으로 제한하지 않는다. `persist-credentials: false`는 checkout 자격 증명을 Git 설정에 남기지 않는 옵션이며 다른 인증 경로까지 제거하지 않는다. 실제 인증 경로와 로컬 생성·커밋·원격 푸시·MCP 쓰기 차단을 S4에서 각각 확인하고, 읽기 전용을 보장한다고 표현하지 않는다. [Action 권한 문서](https://github.com/anthropics/claude-code-action/blob/main/docs/security.md), [설정 문서](https://github.com/anthropics/claude-code-action/blob/main/docs/configuration.md).
- 공식 "Code Review"(워크플로 없이 PR 자동 리뷰) 기능의 조건과 비용. 이 초안은 사용자가 정한 "호출 시에만" 방식을 따른다.
- 구독 토큰을 CI에서 쓰는 것의 한도·약관 세부. 공식 문서는 지원한다고 적지만 사용량 한도는 요금제에 따른다.

## 7. 보안 메모

- `rule-check.yml`은 `pull_request` 이벤트이며 시크릿이 없다. 외부 포크 PR이 와도 시크릿이 노출되지 않는다.
- `claude-review.yml`은 댓글 작성자에게 저장소 쓰기 권한을 요구한다(Action 기본 동작). 이 이벤트는 기본 브랜치의 워크플로 파일을 쓰므로 PR 브랜치에서 파일을 바꿔도 시크릿 접근 코드를 바꿀 수 없다.
- 단, PR이 바꾼 `CLAUDE.md`는 리뷰 시 읽힌다. 그래서 규칙 검사가 `CLAUDE.md`, `.github/`, 검사 스크립트 변경을 경고로 올린다. 이런 변경은 사용자가 직접 본다.
- 워크플로의 사용자 입력은 `env`로 전달해 스크립트 주입을 피한다(`rule-check.yml`).

## 8. 롤백

1. `.github/workflows/claude-review.yml`, `rule-check.yml` 삭제(Codex가 PR로, 또는 GitHub에서 Actions 비활성화)
2. 저장소 Secrets에서 `CLAUDE_CODE_OAUTH_TOKEN` 삭제. 토큰 자체가 계속 유효하므로 필요하면 구독 쪽에서 토큰을 폐기한다.
3. 다른 Claude 기능에 쓰지 않으면 GitHub App 제거
4. `check_github_ci.py`를 바꿨다면 변경 커밋 되돌림은 별도 확인 후 진행

## 9. 2단계 (지금은 하지 않음)

다음 중 하나가 생기면 검토한다.

- 외부 사용자가 패키지를 쓰기 시작해 "깨끗한 환경에서 통과"의 증거가 필요하다.
- 릴리스 전 독립 검증이 필요하다.
- 로컬 테스트 증거와 실제 결과가 어긋난 일이 생겼다.

2단계 후보: GameCI(`game-ci/unity-test-runner`)로 EditMode/PlayMode 실행. 필요한 확인 항목:

- Unity 6000.3.18f1 이미지(`unityci/editor:ubuntu-6000.3.18f1-base-3.2.2`)로 실제 동작하는지(이미지 존재만 확인했다)
- Personal 라이선스 활성화가 현재도 되는지(`.ulf` 파일 방식, 2023년 Unity의 수동 활성화 중단 이슈가 있었다)와 CI 사용 약관
- 브랜치 보호에서 "테스트 통과 필수"만 걸고 승인 방식은 사용자 채팅 확인으로 유지할지. `check_github_ci.py`와 `AGENTS.md`의 병합 규칙을 함께 고친다.
- 병합 방식은 `AGENTS.md` 규칙대로 **fast-forward 또는 일반 merge**(squash 금지). 브랜치 보호의 허용 병합 방식도 이에 맞춘다.
