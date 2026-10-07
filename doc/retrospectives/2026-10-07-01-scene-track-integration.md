# 2026-10-07 · GameSceneManager 최종 수락·main 통합

- 기준: `codex/game-scenes-track`/`6355b36a8edd73ef4c8c87b679f46018e780736a`, main `e9fa4e46f1dc8fe19800800a2229668cb8b4a432`. 같은 작업의 통합 단위로 track을 재사용한다.
- 수락: 사용자가 코드와 최종 Unity 실행 항목까지 확인 완료했다고 명시했다. [확인 기록](../validation/scene-integration/final-integration/acceptance.json)을 보존한다.
- 변경: 수락·현재 상태 문서와 영속 증거만 갱신한다. 재실행된 Editor가 missing SampleScene GUID를0으로 만든 단일 정규화를 확인해 원래 검증된 bytes로 복원했다. 게임/코어/테스트/의존성의 변경은 없다. 사용자 dirty4를 보존한다.
- 검증: 새 테스트 실행0건, 현재 입력283개의 hash를 기존 positive Edit240/240·Play201/201 및 native 두 Player 각10관찰과 대조한다. [P6 증거](../validation/scene-integration/README.md)가 실행 범위를 소유한다. exact source tip CI는 미구성으로 확인했고 CI 성공으로 표시하지 않는다. PID52320 Unity 저장을 정상 확인했다.
- 통합: 원격 main이 기준과 일치하고 track의 모든 원본 commit이 도달 가능한지 검사한 뒤 FF main push한다. 승인된 이번 track/Phase8개의 로컬·원격 tip과 worktree를 대조하고, 원격은 exact SHA lease·로컬은 -d로 정리한다. 다른 작업 브랜치는 제외한다.
- 종료: 최종 저장·정상 Editor 종료를 확인한 뒤 사용자 요청의 PC 종료를 진행한다. 정상 종료 실패 시 강제 종료하지 않고 승인된 offline/sleep fallback을 따른다. 실제 Git SHA/정리 결과는 최종 기록과 보고에 연결한다.
