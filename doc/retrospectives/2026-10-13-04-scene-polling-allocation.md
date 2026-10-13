# IMP-05 · 씬 폴링과 caller buffer

2026-10-13. 구현/자동 검증 완료·사용자 수락 및 PR #10 병합 승인 완료. branchcodex/imp-05-scene-polling, base main3ee358f, testedsource `0cc919b6c497f348deae3e6ae4dffb33f21c8482`.

- 선행: PR9 사용자승인후 수락문서5개만추가, 최종90df125 CI확인·일반merge3ee358f·helperbranch양쪽삭제. source13canonical/GUID/dirty6보존(신규meta5checkout줄바꿈정규화). 버퍼API3개이름/형식을질문했고사용자가확정하면서실패buffer/기여도/패리티·0event검증을추가지시했다.
- 역할: 부모가test/Unity/문서/Git/최종검증, 기존imp01_pool_impl(6.1-sol/medium)이Runtime1file구현후완료, 기존imp04_survey_verification(6.1-sol/high)이private제약분석·임시beforeprobe작성·Red전test리뷰/Green정적리뷰후완료. 공유source동시수정/새사용자thread없음.
- 결정: null→thread검증뒤Clear/fill, 읽기/Add실패이면empty·원래예외·capacity미복원. caller가capacity/버퍼수명소유. 기존속성allocsnapshot유지, CanProceed는내부직접loop로해결한다. expected는무필터union이며actual↔expected양쪽을확인해외부retainedunload를숨기지않는다. shortcircuit/중복set/instance/원래root준비순서유지.
- TDD: 새13case API부재7fail/기존의미6pass, 기존CanProceed2300eventsfail. frozen6이후runtime만수정해서새13+기존71=84/84, 전체359/332=691/691·skip0. typedreflection은Red컴파일가능성과window밖binding용,실제오류전파/반복은typeddelegate. [실제증거](../validation/imp-05/implementation/README.md).
- 배운점: worker sentinel이정상출력과같으면늦은threadguard를검출못한다. 리뷰후Red전default2개전체불변으로고쳤다. requester누락compile오류도Red전실제API에맞춰수정했다. 예상Console집계는base stack문자열대신첫줄별로분류한다. Green복구는동일invocation의fresh결과만허용했다.
- 실측: before100회AllILproxy200/Expected700/Inventory1400/전체2300; after직접helpers/전체0events. beforeproxy는getter193B IL검증후Emit재현이며실제부분block실행과구분한다. 새API각1000회0, legacyOwned/Registered각100회600/400events. buffers3scene/capacity8合算1000회0. 보조bytes는global이며CPU/Player/IL2CPP미검증.
- 범위: runtime1file/557원본중556·dirty6·고정test/helper보존, 새2meta GUID유일. 기존async/P1LINQ/클래스분리/패키지손대지않았다. XML·사람/AI·기능·백로그동기갱신, 테스트용임시Assets4개씩실행뒤삭제/증거보존.
- Git/다음: sourcecommit완료, 문서최종HEAD의규칙/Actions를확인하고PR제공. 작성 당시 main병합/branch삭제/다음IMP03은 승인 대기였으며, 아래 후속 수락 기록을 따른다. 코드업데이트나API이름변경필요가생기면재협의한다. 새Player/IL2CPP·UX·Release·CPU·OOM주입미실행. 이요청은Unity/PC종료를지시하지않았다.

### 후속 수락 · 2026-10-13

사용자가 PR #10 병합을 승인했다. 승인 tip `56c101fb282e7f2723ad6a06a253a741ccf47c4e`와 main `3ee358f`를 원격과 대조하고 정확한 승인 tip의 CI success를 확인했다. 동일 작업 브랜치를 재사용해 승인 기록 문서5개만 갱신하며 runtime/테스트/dirty6 보존을 확인한다. 문서 최종 HEAD의 규칙과 Actions를 확인한 뒤 일반 merge·main 동기화·해당 branch 정리를 진행한다. 최종 SHA와 실제 결과는 PR/Git 이력 및 완료 보고에서 확인한다. 다음 IMP-03은 자동 큐를 따른다. 초기 혼합 PowerShell 출력에서 Editor 부재로 읽은 판단은 정정했다. 별도 PID 조회에서는 Unity 프로세스가 존재했고 CLI status도 ready를 표시했으나, 고유 응답을 요청한 실제 `unity-cli exec --project <절대 checkout>`가 30초 동안 응답하지 않아 subprocess.TimeoutExpired로 종료됐다. IMP-03은 이 실행 사전 확인에서 중단하며 Red 실행0·runtime/test 변경 없음으로 구분한다. 기존 Editor의 실제 명령 응답이 복구돼야 재개할 수 있다. 검증 수치·계측 한계·미실행 범위는 원래대로 보존한다.
