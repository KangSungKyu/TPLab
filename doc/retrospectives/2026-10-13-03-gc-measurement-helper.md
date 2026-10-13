# IMP-14 · GC helper와 Editor 계측 정정

2026-10-13. helper 검증 완료·사용자 승인 대기. 전체 회귀는 CanProceed Red1 때문에 Partial.

- 요청: IMP-05보다 먼저 대조 내장 측정 helper·기존4개 교체·반복 안정성·IMP-01/02 계측 정정을 수행한다. 기준 main9285805, 선행 조사 dd8e487, 브랜치 codex/imp-14-gc-measurement-helper, 검증한 source `1b123d6a77f62e3bc7f75d96a2cb607b403d291a`. 런타임/API 설계·구현은 제외.
- 배정: 부모가 테스트/Unity/Git/문서를 소유했다. 기존 imp01_pool_impl(gpt-6.1-sol/medium)은 helper 한 파일 구현 후 완료, 기존 imp04_survey_verification(gpt-6.1-sol/high)은 읽기 계약·diff 리뷰 후 완료. 공유 파일 동시 수정·새 사용자 세션·재위임은 없었다.
- 결정: sample.Count와 CollectOnlyOnCurrentThread를 주 계측으로 사용한다. 같은 호출에서 static4KB 양성·음성 보정이 통과해야 결과를 낸다. frame bytes는 전역 보조이고 결과 대체가 아니다. 미지원/미검출/음성오염/capacity소진을 실패로 드러내고 workload/recorders를 finally로 정리한다.
- 실제 결과: 기존4개 전부 old counter pass였다. 양성대조 helper baseline에서 실제Red17/pass2/fail15. Green helper13/13·pool28/28. 두 mode N100에서 양성100%·음성0, worker4KB 격리 event0. 전체Edit359/359·Play318/319·skip0. CanProceed100회2300 events의 실패는 IMP-05 Red로 남겼다. [보고](../validation/imp-14/README.md).
- 문제/해결: 보조 frame·1회 호출 보호가 빠졌다는 리뷰를 받아 별도2개 diagnostic Red를 추가했다. 추가 파일은 초기 Red 묶음 실행 중 작성되어 source 동결 운영을 엄격하게 지키지 못했으나 기존 실행 파일/baseline은 변경하지 않았다. 별도 refresh 후 실제Red를 실행했고 이후 Green/회귀에서는 모든 test 소스를 동결했다. 다음에는 초기 fixture 실행 전에 리뷰를 완료한다. 첫 Green은 upstream의 Category.Any가 설치 Unity에 없어 CS0117로 미시작했다. helper만 Memory로 수정, compile 해소 확인 후 실제 실행했다. 설치 버전의 API를 우선 확인한다.
- 정정: 기존IMP-01/02 raw JSON·hash·로직 결론을 유지했다. 과거0B/pass는 계측의 역사적 관측으로 제한하고 이번pool3개의 유효 대조 기반0events를 별도로 기록했다. 신규meta5 GUID유일, baseline547 중 승인된 test3개만 변경, 사용자dirty6/고정Red12 보존. 원래씬/Profiler 상태복원·Console 예상10예외 대조.
- Git: 테스트/helper source commit 완료. 문서·증거를 같은 단위에서 commit/push·main대상PR로 제공한다. 최종 HEAD의 규칙/Actions는 PR·완료 보고에서 확인한다. main병합/branch삭제/다음항목 구현은 하지 않았다.
- 다음: 사용자helper승인 이후 IMP-05의 남은 API 확인/구현을 진행한다. 전체회귀 실패1을 해당 단계에서 Green으로 바꾼다. Player/IL2CPP/다른Unity·플랫폼/소비프로젝트/제품UX·비동기중첩helper는 NotRun.

작업별 Temp/imp-14의 원본은 영속증거 보존·최종검사 이후 명시 목록으로 정리한다. 다른 작업 Temp는 건드리지 않는다.
