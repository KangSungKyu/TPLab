# 2026-10-13 · IMP-05 측정 수단 검증 조사

- 사용자 승인 범위는 양성 결과의 static 보존·반복과 후보별 양성/음성 비교, 기존 GC0 테스트의 양성 대조 유무 목록뿐이다. 기존 branch codex/imp-05-scene-polling-gc/HEAD32b052b를 재사용했다. runtime/test 수정·API 확정·새 패키지·자동 다음 단계는 제외했다. [조사 보고](../validation/imp-05/measurement-survey/README.md).
- Assets 밖의 임시 C# probe를 설치된 CLI의 내부 compile 경로로 memory assembly에 로드했다. 부모만 같은 Editor의 EditMode·빈 씬 PlayMode를 조작했다. static payload 배열과 관측 배열에 결과를 보존했고 mode마다 양성배열88개(워밍업8+실측80)를 고유 참조/내용으로 검증했다. 후보4개·대조2종·각20·mode2=320관측, 별도warmup32. NUnit 실행0.
- 두 mode에서 current-thread.NETcounter는양성0/음성0, GC.Alloc eventCount는1/0, global frame counter는4128B/0, MonoUsed는8192B/0이었다. 이벤트Count와TimeNanoseconds인Value를구분했다. ByteCounter/currentthread/heap사용량은동일계약이아니며 측정방식을확정하지않았다. nativecounter의InternalCall/IL없음만으로stub원인을확정하지않았다.
- 기존 /root/imp04_survey_verification(gpt-6.1-sol/high)을 재사용해 Core/Input/UI의GC0목록·실험scope/static유지/단위를읽기전용검토했다. GC0테스트4개모두내장양성대조없음, TryReturn binder는계측대조가아니다. child수정·Unity/Git실행0. 이전기능테스트의통과결과/문서원문은수정하지않았다.
- 원래InitScene·Profilerfalse·EnterPlayMode설정을복원했다. tracked547와dirty6 rawhash일치·고정Redfile불변·제품Consoleerror0. 첫복원snippet은Jsonextension사용오류로컴파일단계에서1회실패했고 실행되지않았으며cast수정뒤복원됐다. 임시probe소유자원은Dispose·domainreload로정리했다.
- Player/IL2CPP/다른Unity/다른스레드부하/기존tests재실행·수정/Green·전체회귀는미실행이다. .NETcounter의0만으로현환경무할당을입증할수없고Native원인은미확정이다. 조사문서·증거·회고·색인만갱신하며main/PR/Actions자동통합은없다.
- 결과보고후멈추라는최신지시를지킨다. 이번Temp/imp-05-measurement의소유파일은영속증거보존·복원확인뒤정리하고, 기존Temp/imp-05는별도재개용이라보존한다. 현재Unity/PC종료권한은없다. Git문서commit/push와후속사용자확인은구분한다.
