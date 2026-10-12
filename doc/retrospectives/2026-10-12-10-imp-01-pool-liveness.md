# 2026-10-12 · IMP-01 외부 파괴의 지연 슬롯 회수

초기 단계는 후보 `6e16dc4`의 기록이다. 현재 후속 소스 `901d56c582691d55f4aa9698e91be57231328635`와 최신335/309 회귀·명시 승인된 백로그 상태 수정은 아래 보완 절을 따른다.

## 작업과 기준

- 기준 main `a10c3022587a46b0cc3d479a2273c82ec649611f`, 작업 `codex/imp-01-pool-liveness`, 검증 소스 `d7f319c6f18713bb9fba77dd9505fc9dbe26f088`.
- 상태: 자동 검증 완료·사용자 확인 대기. PR #6(IMP-04) 병합 승인에 따라 백로그 큐의 다음 단위 D-01=A1만 진행했다. IMP-02는 승인 전 시작하지 않는다.
- [검증·변경·한계](../validation/imp-01/README.md), [백로그 원문](../IMPROVEMENT_BACKLOG.md), [선행 IMP-04](2026-10-12-09-imp-04-apply.md).

## 결정과 변경

- ObjectPool 마지막 optional isAlive 및 생성자 scratch, Peek 보존·정원 시 대여 점검·정리 오류 집계로 사망 슬롯을 지연 회수한다. PrefabPool은 fake-null predicate를 주입한다. 정상 reuse의 전체 loan 순회·추가 컴포넌트를 피하고 System-only 경계를 유지했다.
- Count의 지연 정확성, predicate 예외/재진입, dead factory 정리, 실패 cleanup 재시도 없음, source-compatible/미리 컴파일한 소비 어셈블리 재빌드 필요를 XML·사람/AI 문서에 함께 기록했다. Return/Dispose 본문은 유지했다.
- 원문 백로그의 '정상 경로 비용0'은 CPU까지0을 뜻하지 않는다. 각1000회 strict GC0 통과 범위와 predicate 비용·initial scratch/factory/콜백 비용을 분리했다.
- 부모가 고정 Red·Editor·검증·문서·Git을 담당했다. `/root/imp01_pool_impl`(gpt-6.1-sol/medium)은 core2·보충test/meta2·pooling docs4만 구현했다. `/root/imp04_survey_verification`(gpt-6.1-sol/high)은 권한·diff·소유권/실패/할당 계약을 읽기 전용 리뷰했다. 두 하위 작업 완료, 중요 미해결 결함 없음. 하위 작업의 추가 Unity/Git 실행은 없다.
- 단일 실제 Editor에서 refresh 후 새 ctor/test reflection을 확인했다. CLI reload 반환1은 같은 실행의 fresh connector 결과로 복구되어 도구 exit0·실제 이름/수/결과를 구분했다. 다른 Editor·suite·반복 실행으로 대체하지 않았다.
- 부모 정적 검사에서 Windows 상대 경로 separator 비교와 무관한 사용자 씬 공백 포함 검사 범위를 바로잡았다. 소스·사용자 자산 변경 없이 소유 경로/GUID·작업 diff만 검사했다.

## 검증과 보존

- 실제 Red Prefab18(pass17/fail1/skip0): Expected True / But was False. Red 파일은 이후 SHA 고정, 기대값·Ignore·timeout 수정 없음.
- Green Prefab18/18·기존generic24/24·보충15/15. 전체 Edit334/334·Play307/307, fail/skip0. 두GC 테스트 각1000회0바이트. 새 fixture 전체를 Red 실행했다고 주장하지 않는다.
- 실제 compile/loaded code·테스트 종료 상태 확인, Console 예상예외10건을 LogAssert와 대조했다. Console 로그0이라고 주장하지 않는다.
- 원본504 중 수정한source3 외501개, dirty6, fixedRed, Green후source5 raw hash 보존. 새metaGUID유일·기존GUID/Return/Dispose·backlog 원문32772바이트 보존.
- 새 소비 프로젝트·Player·IL2CPP·다른 플랫폼/Unity버전·시각UX·문서발췌 예제는 미실행. 기존 역사적 소비 결과로 이번 변경을 확인했다고 하지 않는다.

## Git와 다음 작업

- PR #6: 승인된 tip `0e18af4db2631488f34f4e00cdd7b1f61cf1ef11`, exact Actions 성공 후 main `a10c3022587a46b0cc3d479a2273c82ec649611f` 일반 merge. 로컬/원격main 일치·tip 도달성 확인 후 해당 브랜치 양쪽 삭제 완료.
- IMP-01은 source commit 뒤 source revision을 문서에 연결하고 문서·검증·회고 commit을 같은 PR로 제공한다. 최종 HEAD 규칙/Actions 결과와 링크는 완료 보고·PR 본문이 소유한다. source-only 상태를 외부 배포하지 않는다.
- 작업별 Temp/imp-01의 필요한 증거만 위 검증 폴더에 보존하고 이번에 만든 명시 파일을 완료 후 제거한다. 사용자 승인 대기는 PR·영속 문서로 재개할 수 있다. 기존 Temp·dirty·Unity 상태는 보존한다.
- 최종 사용자 확인 이후에만 이 PR을 병합·브랜치 정리하고 IMP-02를 시작한다. 이 단위에 Unity/PC 종료·절전 권한은 없다.

## 2026-10-12 명시 후속 요청 보완

- PR #6 merged/main a10c302 확인 후 같은 PR #7/branch를 재사용했다. latest main의 백로그 증거4곳은 정확했으며 작업 구현 뒤 ObjectPool 행만 이동했다. [현재 보고](../validation/imp-01/README.md#2026-10-12-후속-요청--현재-경계-검증과-상태-갱신)를 참조한다.
- 기존 fixedRed·구현·15case는 동결하고 새 경계3개를 별도 fixture에 고정했다. private CreatePool을 재사용하려면 Red 파일 변경이 필요하므로 별도 source setup/teardown만 만들었다. main body를 namespace만 변경한 임시 clone로 같은 Editor에서 사후 비교하고 원문·hash 범위/결과를 보존했다. 원래 checkout을 rollback하지 않았다.
- baseline: alivePASS, inactiveFAIL(실제 pooled clone destroyed 예외), 생략정책EditPASS. 현재:3/3와 기존Prefab18/18(필수5포함). 보존형 테스트를 인위적으로 Red로 만들지 않으며 사후 비교를 사전TDD로 주장하지 않는다. 임시 clone/meta8개는 명시 경로만 제거했고 최종 assembly/test 목록에 없다.
- 부모가 Green filter를 클래스명 일부로 지정하여 0건이 선택됐다. 이를 통과로 세지 않고 결과·driver·실제 idle 상태를 기록했다. 설치 필터를 확인한 뒤 exactfixture로 별도 실행했다. 테스트 기대값/Ignore/timeout/구현을 바꿔 통과시키지 않았다.
- 이후 XML2줄의 지연 Count 설명만 보완해 core/test9개를 다시 동결했다. 전체Edit335/335·Play309/309, fail/skip0. 이전641개 이름보존·신규3개를 확인했고 최종GC2개1000회strict0도 통과했다. compile/loaded API·Console예상10·dirty6/frozenRed/meta 보존을 확인했다.
- 백로그 상태6곳 변경은 이번 사용자 명시 지시로 허용됐다. 원문전체 바이트 동일을 현재에 주장하지 않는다. 원문은 초기tip에 보존하고 D01/기타302행은 유지했다. source/helper/framework/패키지·다른IMP 변경은 없다. source commit 뒤 XML·사람/AI 문서·검증·회고를 같은 PR로 제공한다.
- 읽기 전용 verifier(/root/imp04_survey_verification, gpt-6.1-sol/high)는 main위치·보존형vs변경형·newtests·상태6곳을 검토했고 중요문제없음. 부모만 Unity/Git을 조작했다. 하위 추가실행없음.
- Counts의 지연 한계 유지·공개Prune은 별도 사용자 결정으로 보류. 신규Player/IL2CPP/소비설치/다른플랫폼/시각UX·예제는 미실행. 최종HEAD 규칙/Actions는 PR/완료보고로 확인한다. 병합/forcepush/branch삭제·IMP02시작은 이 요청에서 수행하지 않는다. 새 shutdown/절전 권한도 없다.
- 이번 Temp/imp-01-followup 증거는 영속validation JSON으로 보존하고 사용한 명시파일만 완료후 정리한다. 예전Temp/사용자작업은 건드리지 않는다.
