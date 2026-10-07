# Scene loading final integration

2026-10-07. 사용자가 “아 이해했어 그리고 playmode로 확인 했어”라고 로딩 UI PlayMode 확인을 전달했다. 최종 사용자 확인 gate를 완료로 반영한다. 개별 모드·해상도·물리 장치별 결과는 제공되지 않았으며 해당 항목을 자동으로 검증 완료로 확대하지 않는다.

기준 track `e4d1f560ee6c73a4af7374bc9fb98eff729a27ea`, 원격 main `32e6cac1e80c95ec38033f95310f7ac2459e095f`. 같은 작업 track을 재사용한다. [검증 입력](../p4/test-inputs.json) 317개·보호 raw7·GUID·링크·실제 결과 수가 현재 checkout과 일치한다. 기존 Edit271/271·Play253/253·reload12/12 및 두 Player각12/12는 동일 실행 source의 증거다. 이번 단위는 문서와 Git만 변경하므로 Unity 테스트는 새로 실행하지 않는다(0건; 테스트 통과로 세지 않음).

[작업 phase tips](phase-tips.json)를 원본 그대로 보존한다. main을 track에 포함하는지 확인하고 fast-forward main push 이후 main/원격일치·각 tip의 main 도달·다른 worktree 미사용을 검사한다. 삭제 직전 서버 tip을 다시 확인하고 그 SHA를 `--force-with-lease=<ref>:<sha>` 조건으로 원격 ref만 삭제한다. 로컬은 `git branch -d`를 사용한다. 강제 이력 재작성이나 다른 branch 삭제는 포함하지 않는다.

실제 삭제 결과는 후속 `phase-cleanup.json`에 보존한다. track 삭제는 모든 문서·검증 기록의 최종 main push 이후에만 진행하며 최종 track tip은 main의 동일 SHA와 최종 보고로 확인한다. SHA에서 branch를 다시 생성할 수 있도록 fast-forward로 원본 commit을 main에 보존한다.

원격 Addressables content, IL2CPP/다른 플랫폼, 기록되지 않은 개별 장치·해상도 coverage의 한계는 유지한다. 이번 확인은 새 PC 종료/절전 지시가 아니다. 기존 사용자 dirty4를 보존한다.

## 실행 결과

사용자 확인 문서 commit `930ddeccc35d3fe5ecb8e4c81b91a20f70fca356`을 main에 fast-forward push하고 로컬/원격 일치를 확인했다. 단계 브랜치3개는 [실제 삭제 결과](phase-cleanup.json)에 기록된 원본tip을 main에 보존한 뒤 SHA 조건부 원격 삭제와 로컬 `-d`로 제거했다. 남은 track은 이 마감 문서를 포함하는 최종 main push 뒤 동일 SHA를 조건으로 정리한다. 추가 runtime/test/asset/configuration 변경은 없다.
