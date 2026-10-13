# IMP-05 · 씬 폴링과 caller buffer

2026-10-13. 구현/자동검증완료·사용자승인대기. branchcodex/imp-05-scene-polling, base main3ee358f, testedsource `0cc919b6c497f348deae3e6ae4dffb33f21c8482`.

- 선행: PR9 사용자승인후 수락문서5개만추가, 최종90df125 CI확인·일반merge3ee358f·helperbranch양쪽삭제. source13canonical/GUID/dirty6보존(신규meta5checkout줄바꿈정규화). 버퍼API3개이름/형식을질문했고사용자가확정하면서실패buffer/기여도/패리티·0event검증을추가지시했다.
- 역할: 부모가test/Unity/문서/Git/최종검증, 기존imp01_pool_impl(6.1-sol/medium)이Runtime1file구현후완료, 기존imp04_survey_verification(6.1-sol/high)이private제약분석·임시beforeprobe작성·Red전test리뷰/Green정적리뷰후완료. 공유source동시수정/새사용자thread없음.
- 결정: null→thread검증뒤Clear/fill, 읽기/Add실패이면empty·원래예외·capacity미복원. caller가capacity/버퍼수명소유. 기존속성allocsnapshot유지, CanProceed는내부직접loop로해결한다. expected는무필터union이며actual↔expected양쪽을확인해외부retainedunload를숨기지않는다. shortcircuit/중복set/instance/원래root준비순서유지.
- TDD: 새13case API부재7fail/기존의미6pass, 기존CanProceed2300eventsfail. frozen6이후runtime만수정해서새13+기존71=84/84, 전체359/332=691/691·skip0. typedreflection은Red컴파일가능성과window밖binding용,실제오류전파/반복은typeddelegate. [실제증거](../validation/imp-05/implementation/README.md).
- 배운점: worker sentinel이정상출력과같으면늦은threadguard를검출못한다. 리뷰후Red전default2개전체불변으로고쳤다. requester누락compile오류도Red전실제API에맞춰수정했다. 예상Console집계는base stack문자열대신첫줄별로분류한다. Green복구는동일invocation의fresh결과만허용했다.
- 실측: before100회AllILproxy200/Expected700/Inventory1400/전체2300; after직접helpers/전체0events. beforeproxy는getter193B IL검증후Emit재현이며실제부분block실행과구분한다. 새API각1000회0, legacyOwned/Registered각100회600/400events. buffers3scene/capacity8合算1000회0. 보조bytes는global이며CPU/Player/IL2CPP미검증.
- 범위: runtime1file/557원본중556·dirty6·고정test/helper보존, 새2meta GUID유일. 기존async/P1LINQ/클래스분리/패키지손대지않았다. XML·사람/AI·기능·백로그동기갱신, 테스트용임시Assets4개씩실행뒤삭제/증거보존.
- Git/다음: sourcecommit완료, 문서최종HEAD의규칙/Actions를확인하고PR제공. main병합/branch삭제/다음IMP03은승인전진행하지않는다. 코드업데이트나API이름변경필요가생기면재협의한다. 새Player/IL2CPP·UX·Release·CPU·OOM주입미실행. 이요청은Unity/PC종료를지시하지않았다.
