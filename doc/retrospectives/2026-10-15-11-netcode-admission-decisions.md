# Netcode P0 사용자 결정과 검증기 주입 세부 계약

위험 등급: **T0 · 문서 전용**. 사용자 결정2026-10-15의 D-N1~D-N8을 기존 [NETCODE_SESSION_DRAFT.md](../NETCODE_SESSION_DRAFT.md)에 반영하고, 입장 검증기 주입의 세부 규격은 제안으로 구체화했다. 구현·새 패키지·설치·컴파일·Unity 실행은 없다.

## 작업 기준·선행 확인

PR #34는 closed/merged=true, 병합 SHA `8b5ddcd4e9896bcd48e5fc22dde6a8cf63514cad`임을 GitHub에서 확인했다. local main/origin/main/서버 main이 모두 같고, repository 전체 open PR 조회 결과0·추가 페이지 없음이었다. dirty10개(추적7·미추적3)의 경로와 원문 SHA256을 snapshot한 뒤 최신 main에서 `codex/netcode-admission-decisions`를 만들었다. 기존 승인으로 이 새 PR을 병합하지 않는다.

허용 변경은 기존 계약 문서·신규 회고1개·회고 INDEX 행1개·doc INDEX의 기존 행 현행화다. C항의 의존성 읽기 전용 조사 결과는 사용자 요청에 따라 **채팅에만 보고**한다. 반영 후보 diff·패키지/manifest/lock 자료를 저장소 문서나 파일로 만들지 않는다. Temp에는 C 후보가 아닌 보존용 원문 해시와 문서 검사 자료만 둔다.

## 결정 반영과 위치

| 결정 | 문서 위치 | 반영한 범위와 남은 단서 |
|---|---|---|
| D-N1 | [A절](../NETCODE_SESSION_DRAFT.md#a-테스트-구조와-ngo-어댑터-경계), [결정 표](../NETCODE_SESSION_DRAFT.md#미확인-항목과-사용자-결정) | 내부 순수 상태기계+얇은 NGO 어댑터. NGO 미설치 컴파일용 별도 assembly는 별도 승인 |
| D-N2 | [B절](../NETCODE_SESSION_DRAFT.md#b-입장-검증-접점과-신뢰-경계), 결정 표 | 프로젝트 검증기 주입. wrapper는 증명 형식 선택 안 함. 상세 규격은 제안 |
| D-N3 | [C절](../NETCODE_SESSION_DRAFT.md#c-패키지배포-연결과-의존성-반영), P0–P5·결정 표 | P1~P5 개발 뒤 UI와 함께 UPM/배포를 맨 마지막에. 그 전 배포 패키지 구성·버전·packager 작업 없음 |
| D-N4 | [D절](../NETCODE_SESSION_DRAFT.md#d-로비-복원-실패-상태의-출구), 결정 표 | 정리 완료 후 명시적 복원 재시도+안전한 종료. RetryLobbyRestoreAsync 이름·멱등 key는 미확정 |
| D-N5 | [E절](../NETCODE_SESSION_DRAFT.md#e-대기-상한과-취소의-서로-다른-수명), 결정 표 | 전체 budget·명단 유지, 개별 연장/일부 제외 없음. 60초는 실험 후보, 종료/복원 값도 미확정 |
| D-N6 | C절·결정 표 | 검토한 package 입력의 별도 승인 단위만 허용. 현재 dirty 전체 커밋/manifest·lock 변경 없음 |
| D-N7 | 결정 표 | Named message 후보로 진행. sender 검증·등록/해지는 P1/P3, public API는 미확정 |
| D-N8 | 미확인 U9·결정 표 | false를 P1 후보로 검증하는 방향만 결정. 최종 초기 동기화 설정은 P1 결과 후 재결정 |

결정 표의8행 각각에 “근거: 사용자 결정 2026-10-15”를 적었다. 기존의1차 범위·명단/실패 복원·GameSceneManager 비확장 결정3문장은 그대로 보존했다. 그 밖의 제안/미확인을 일괄 승격하지 않았다. 머리말은 “P0 사용자 결정 반영, 구현 승인 아님; 구현 없음”이며 기준 source는 읽은 main `8b5ddcd4e9896bcd48e5fc22dde6a8cf63514cad`와 명시된 사용자 dirty 설치 소스다. 갱신일2026-10-15는 실제 내용 변경 작업일이다. P0 표도 “결정 반영, 구현 승인 아님”으로 맞췄다.

## B절 구조와 남은 하위 결정

| 절 | 구체화한 제안 |
|---|---|
| B1 책임 분담 | 프로젝트가 증명 발급·검증·계정/좌석·replay를 담당. wrapper는 검증기 호출 전 외곽 길이/버전, 실제 sender mapping·세대·중복·응답/코드를 담당 |
| B2 입출력 | 가칭 verifier/context, 불투명 바이트·서버 맥락·raw ClientId/접속 세대·시간/budget, 승인 ID/거부 코드. NGO 타입 없는 경계와 A1 연결을 추천하되 실제 public API는 미확정 |
| B3 동기/비동기 | 짧은 동기 우선, Pending은 선택지. 비동기 완료 시 Attempt/Roster/접속/owner/예약/기한 재검사. 늦은 성공은 상태를 되살리지 않음 |
| B4 실패·스레드 | 예외·지연·잘못된 결과를 접속 거부로 정규화. 서버 원인 보존·원격 원문 비노출. 접속 실패와 D-N5 전체 budget 만료를 구분. callback 전 경로의 메인 스레드 보장은 미확인 |
| B5 replay | 증명 진위·일회성 소비·재발급/교차시도 replay는 프로젝트, 같은 Attempt의 살아 있는 mapping/보류 예약·늦은 응답 방어는 wrapper |
| B6 Host | NGO는 Host 로컬 거부를 자동 승인하므로 시작 전 검증. 같은 verifier 재사용/별도 로컬 신뢰 경로 비교. 한 시도 결과 재사용으로 증명 이중 소비 방지 후보 |
| B7 프로젝트 요건 | verifier 구현·안전한 발급 경로·원인/코드·수명 책임. 초기 개발 예제는 P5 후보이며 현재 구현 없음 |
| B8 하위 결정 | S1~S4를 모두 [제안]으로 유지 |

| 하위 결정 | 선택지·추천 | 상태 |
|---|---|---|
| S1 | 동기 우선 / 실제 필요 시 Pending 비동기. 스레드·예약·취소·늦은 완료·기한 포함 | 제안 |
| S2 | 프로젝트 증명 replay + wrapper 같은 Attempt 중복/예약 제한. 부분 소비·재발급은 프로젝트 계약 | 제안 |
| S3 | 전체 payload1KiB는 실험 후보. envelope+proof의 실제 크기·유효 한계 측정 뒤 값 선택 | 제안 |
| S4 | 같은 verifier의 Host 시작 전 호출 추천. 별도 로컬 신뢰 경로와 비교, 결과 수명/이중 소비는 P1 확인 | 제안 |

D-N2가 확정됐다는 이유로 S1~S4·위임 경계의 세부 규칙을 [결정됨]으로 바꾸지 않았다. “프로젝트가 주입한다”와 “그 구현을 특정 ticket으로 정한다”를 구분했다. Host 사전 검증 실패는 시작 요청 거부이며 원격 verifier 거부를 전체 세션의 즉시 실패로 확대하지 않았다. 이미 승인된 멤버 이탈·전체 budget 만료는 기존 실패 경로다.

## 설치 소스 확인과 미확인

B절의 NGO 사실은 기존 N1~N4/N7 안에서 다시 읽었다: NetworkManager의 callback setter/입출력·Pending/Reason, NetworkConnectionManager.ApproveConnection/ProcessPendingApprovals의 직접 호출·Pending 처리, HostServerInitialize의 자동 승인과 비대기, CustomMessagingManager의 실제 sender 접점이다. 이 함수의 직접 호출을 “모든 transport 경로가 항상 Unity 메인 스레드”로 일반화하지 않았다. wrapper 메인 스레드 경계는 제안이고 실제 callback 관찰은 P1 미실행이다. 설치 소스에서 확인하지 않은 API 동작을 새 현행 사실로 적지 않았다.

갱신한 미확인: 실제 프로젝트 verifier/발급·별도 사유 채널, callback 스레드·검증기 실행 위치/입력 수명, payload/Reason 상한·보안 구성, 개발 assembly 참조와 후속 UPM 의존성, 복원 API 이름·멱등 key·부분 실패, deadline/종료·복원 값, P1의 컴파일·Player/다중 instance·NGO 종료 이벤트/프레임, 최종 초기 동기화 설정. false 후보 검증 방향과 최종 설정 확정을 분리했다.

## 검사·명령·실행 범위

환경은 Windows PowerShell·Git·Python3.14이며 기준 source는 `8b5ddcd4e9896bcd48e5fc22dde6a8cf63514cad`다. 임시 자료는 `Temp/netcode-session-decisions-20261015/`에만 둔다. 같은 작업의 preflight/원문 해시·문서 검사 목적이며 설치/컴파일/Unity 테스트 결과가 아니다.

```text
git status --short
git rev-parse HEAD main origin/main
git ls-remote origin refs/heads/main
C:/Python314/python.exe -B Temp/netcode-session-decisions-20261015/verify_doc_links.py --label before/after
C:/Python314/python.exe -B tools/verify_validation.py --evidence <16개 checks 폴더>
C:/Python314/python.exe -B doc/validation/bootstrap-system/verify.py
C:/Python314/python.exe -B tools/check_pr_rules.py --base 8b5ddcd4e9896bcd48e5fc22dde6a8cf63514cad --head HEAD
git diff --check 8b5ddcd4e9896bcd48e5fc22dde6a8cf63514cad HEAD
C:/Python314/python.exe -B tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <40자리 HEAD> --output Temp/netcode-session-decisions-20261015/ci-final.json
```

verifier는 작업 전에 fresh17회(16 checks+Bootstrap1)를 실행해 원래 실패를 보존했다. 전/후 exit/stdout/stderr 원문 전체를 대조하고 기존17건을 현재 검증 성공으로 표현하지 않는다. 보호 입력836경로·설치 소스 원문·사용자 dirty10개의 hash/존재 상태도 비교한다. 문서에 라벨 집계 표는 없으며, 구조 검사로 결정 표8행의 ID·상태·근거와 기존 확정 범위3문장 보존, B절 미확정 제안·S1~S4를 점검한다. 처음 Temp 검사에서 라벨 설명문/기존 확정 범위의 개수를 단순 문자열 수로 예상해 저장 전 중단했고, 실제 행/기존 문장 전체의 보존 비교로 바로잡았다. 계약 문서를 바꿔 검사 실패를 숨기지 않았다.

머리말5필드·유형/상태/source·작업일·계약 링크와 줄 번호 링크0을 확인한다. 코드·기존 테스트·asmdef·tools·workflow·upm·AGENTS·CLAUDE·manifest·lock 변경0과 두 INDEX의 지정 행만 변경됐는지를 diff로 확인한다. 실행하지 않은 것은 설치·컴파일·Unity EditMode/PlayMode·제품 Console·Player/IL2CPP·GC·다중 프로세스·실제 인증/통신·UPM/배포다. 이 새 PR의 main 병합·브랜치 삭제·PC 제어도 하지 않는다.

## 최종 검사와 후속

실측 링크/anchor 검사는 Markdown258개·상대 링크2113개·anchor491개·오류0이다. 작업 전 검사(문서 저장 전)의257/2104/484/0과 비교해 새 오류0이다. verifier17실패→17실패의 전체 exit/stdout/stderr 원문이 동일하고 보호 입력836경로·설치 소스11개·사용자 dirty10개의 hash/존재 상태를 보존했다. 머리말·결정8행의 근거와 기존 확정3문장·B절/S1~S4 미확정 상태·색인 지정 행 검사를 통과했다. 실제 커밋의 공백·규칙·exact-SHA Actions 결과는 아래에 확정한다. PR은 open으로 제공하고 사용자 승인 전 병합하지 않는다. 다음에는 S1~S4와 P1 선행 의존성 반영의 별도 승인 범위를 정해야 한다. 이번 결정 반영이 구현/설치 승인이라는 뜻은 아니다.

검토·보완 재개에 필요한 원문/해시/검사기는 위 Temp 작업 폴더에 유지한다. 사용자 수락 후 더 이상 실행/조사 참조가 없으면 이 작업 소유 명시 목록만 정리하고 다른 Temp와 사용자 파일은 보존한다.
