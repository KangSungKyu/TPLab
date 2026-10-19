# 0.0.2 정식 발행 회고

## 요청·결정과 범위

2026-10-19 사용자가 “설치본 확인 완료, 배포 진행”으로 UI 실제 입력·화면 및 Netcode 실패·복원·종료를 포함한 설치본 수락과 배포를 승인했다. 기존 Core 구조를 유지하고 UI·선택 Input 연동·Netcode를 추가하는 승인 범위다. 배포 후보 변경은 [준비 회고](2026-10-16-08-distribution-002.md)의 T2 단위이며, 이 후속 커밋은 발행 결과와 현재 문서 상태만 갱신하는 **T0**다. 코드·테스트·asmdef·UPM·도구·package 입력은 변경하지 않는다.

## source·병합·발행 대응

| 항목 | 확인 결과 |
|---|---|
| 배포 source / PR #43 HEAD | `bf2721ec50a1029531c4e17b2c307a483a5f6e30` |
| PR #43 일반 merge / 당시 main | `1aac47192fbd5ad8fd89820221063eee4c294e97`; merge tree와 source tree 일치, 로컬/원격 main 일치 |
| annotated tag | `v0.0.2`, tag object `18790966c01f1eb40e9f0baa5ccc3e93e1326124`, peeled source는 위 bf2721e |
| 공개 Release | [v0.0.2](https://github.com/KangSungKyu/TPLab/releases/tag/v0.0.2), ID `416801446`, 정식/latest, 2026-10-19 00:44:46 UTC 공개 |
| 첨부물 | package `.tgz` 6개 + SHA256SUMS·manifest·INSTALL·VALIDATION·RELEASE_VALIDATION 5개 |
| branch 정리 | `codex/distribution-002-ui-netcode` 로컬/원격 삭제; 삭제 전 tip bf2721e가 main에 포함되고 다른 worktree의 사용이 없음을 확인 |
| 이번 문서 branch | `codex/release-002-close`, base 1aac471; 마지막 문서 HEAD와 CI·통합 결과는 해당 PR Checks와 완료 보고에서 확인 |

배포 tag는 merge commit이나 이후 문서 commit으로 옮기지 않는다. detached 배포 worktree에서 exact source를 다시 생성한 6개 archive는 기존 최종 후보와 byte/hash가 모두 같다. publication snapshot도 일치했다. 이번 문서 변경은 manifest의 package source-input 목록과 교집합이 없으므로 발행 package를 재생성하지 않는다.

## 실제 실행과 검증 경계

| 검증 | 이번 발행 단계의 결과 |
|---|---|
| 병합 전 exact-SHA CI | bf2721e의 규칙 검사/Actions 조회 complete·passed. 조회를 병합 직전에 재확인 |
| 태그 설치 | fresh consumer의 TPLab Git provider 6개가 실제 `#v0.0.2`를 사용. lock의 source 대응, package 0.0.2 및 payload 422파일 일치 |
| 예제·컴파일 | Core/Input/UI/Netcode 4개 sample import, assembly/GUID·compile 확인. import·verify 두 batch Editor가 exit0, timeout false |
| 태그 설치 시간 | import 113.253초 + verify 7.711초 = 120.964초. 프로세스 시작·import·compile·종료 포함 |
| draft 다운로드 | upload한 11개 전부 다시 내려받아 이름·길이·SHA256 일치 |
| 공개 다운로드 | 인증 없는 public URL에서 11개 전부 다시 내려받아 길이·SHA256 일치 |
| 사용자 수락 | 위 설치본 직접 확인 완료 답변. 자동 import/compile과 별도 gate |
| 이번 전체 Unity 테스트·Player 실행 | 미실행(각 0회). 후보 검증을 이번 태그 실행으로 세지 않음 |
| 문서 마감 | Markdown 299개 링크/anchor 오류0, 머리말3개 형식·문서7개 공백 검사 통과. 규칙·최종 문서 HEAD Actions 결과는 해당 PR/완료 보고 |

Windows / Unity 6000.3.18f1에서 실행했다. `tools/build_distribution.py`의 기존 생성 경로와 `tools/run_package_consumer.py`를 재사용했다. 작업 전용 `run_tag_consumer.py --execute --unity <6000.3.18f1/Unity.exe> --output <consumer> --evidence <tag-install>`은 기존 계획의 TPLab URL 6개만 태그로 바꾸며 전후 원격 tag peel을 확인한다. 설치 계획의 source는 bf2721e로 유지한다. 원본 TPLab Editor 테스트 대신 다른 Editor를 사용한 것이 아니라 배포 규격의 독립 설치 consumer를 실행한 것이다.

후보 단계의 전체 EditMode 534/534·PlayMode 427/427, Git/tarball 16구성, UI Player와 Netcode loopback 실행은 [준비 회고](2026-10-16-08-distribution-002.md)의 source별 근거·최초 실패·한계를 따른다. 특히 Canvas 초기 0 Draw Calls 프레임과 격리 재실행을 구분하며 안정적인 성능 보장으로 확대하지 않는다. 이번 태그 consumer는 Player·시각 확인을 실행하지 않았고 사용자 수락은 별도 답변에 근거한다. IL2CPP·다른 플랫폼·Relay/WAN·실제 인증 provider·새 GC 계측은 미실행이다. 역사적 verifier 17건은 이번 문서 마감에서 재실행하지 않았다.

## 발행 도구 검토와 문서 변경

부모가 Git·Unity·Release를 소유하고 기존 `/root/distribution_docs_review` 하위 에이전트는 읽기 전용 발행 경계 검토와 작업 전용 태그 runner·링크 검사기를 맡았다. 기존 에이전트를 재사용했으며 이번 재개에서 모델 override를 요청하지 않았다. 동일 파일 동시 수정은 없었다. 배포 API 호출은 기존 git credential helper의 인증을 메모리에서만 사용하고, 허용 host와 redirect의 cross-host Authorization 제거를 확인했다. 비밀값은 로그/문서/배포물에 저장하지 않았다.

독립 검토가 SHA256SUMS 파일의 존재 검사만으로는 내용의 유효성을 보장하지 못함을 발견했다. 외부 발행 전 정확한 6개 파일명·해시·중복/누락/추가/형식을 검사하도록 임시 도구를 보완했다. 정상 1 + 오류 입력 5 = 6개 자체 검사 통과 후 draft를 생성했다. 이는 제품 runtime 테스트나 TDD Red를 뜻하지 않는다. 새 링크 검사기의 첫 75건은 inline-code 예시와 GitHub heading slug 처리 오류였다. 파서 정정과 자체 검사 10/10 후 전체 Markdown 299개 오류0을 확인했으며 문서를 검사기에 맞춰 바꾸지 않았다. checksum 검사 미존재를 드러낸 최초 검토 스크립트 오류도 로컬 검증 기록에 보존한다. draft/public 검증이 끝난 11개 첨부물은 덮어쓰지 않는다. `RELEASE_VALIDATION.json`의 prepublication 결과와 공개 후 다운로드 결과는 별도 기록이다.

README와 배포 규격은 정식 설치 안내를 추가하고, CORE_PLAN의 UI 배포 후속 항목과 NETCODE_SESSION_DRAFT의 패키지/통합 상태만 현재 사실로 맞췄다. Netcode의 실제 의존성은 NGO 2.13.3·UniTask 2.5.11·Collections 2.6.6이며 Core/UI/Input은 없다. NGO 간접 의존 Transport 2.7.3을 소비 lock에서 확인했다. 기존 P0/P3 관찰·0.0.1 이력과 선택 개선 항목은 보존한다. 문서/회고 색인에 이번 발행 행을 추가한다.

## 보존·정리와 한계

사용자 dirty는 tracked 9 + untracked 3 = 12개다. 씬·렌더 설정·InputActions·Build/Project/Cloud 설정과 DefaultNetworkPrefabs/SceneTemplateSettings의 시작 SHA256을 저장하고 작업 후 동일성을 확인한다. 커밋에는 이번 문서만 포함한다. PR #43 branch는 main 도달 가능성·서버 tip·worktree 사용 여부를 확인한 뒤 조건부 원격 삭제와 `git branch -d`로 정리했다. 배포 source는 main과 v0.0.2에서 계속 도달 가능하다.

재개 시 이전 `Temp/distribution-002-20261016` 원시 자료는 존재하지 않았다. 소실 원인은 확인하지 못했으며 원시 파일을 이번에 재열람했다고 주장하지 않는다. 기존 committed 회고와 남아 있는 최종 archive/manifest를 근거로 후보 결과를 구분하고, 이번 발행은 새 태그 설치·재생성·다운로드를 직접 실행했다.

이번 raw JSON·로그·임시 실행 도구는 `Temp/distribution-release-002-20261019`에서 생성했다. 완료 시 보존할 로컬 증거는 ignored `tplab/releases/0.0.2`의 archive로 옮겨 원문/해시를 검사하고 작업 소유 Temp만 정리한다. 이 경로는 외부 소비 문서의 필수 입력이 아니며 공개 Release의 설치/검증 안내만으로 설치할 수 있다. 배포 source detached worktree는 이전 후보 재현용 cache로 유지하며 별도 정리 시 정확한 source와 ignored 산출물 보존부터 확인한다. 새 기능·성능 최적화·Core 기능별 assembly 분리는 진행하지 않았다.
