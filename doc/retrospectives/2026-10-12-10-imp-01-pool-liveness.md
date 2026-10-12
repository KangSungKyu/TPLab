# 2026-10-12 · IMP-01 외부 파괴의 지연 슬롯 회수

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
