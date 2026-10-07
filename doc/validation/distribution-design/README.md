# 배포 규격·public 제공 설계 검증

2026-10-07. 기준 main `993a0617b8b5253175d9a225432f0aa642d19d3d`, 작업 `codex/distribution-design`.

문서와 라이선스 작업이다. `Assets`·`Packages`·`ProjectSettings`·실행 도구를 수정하지 않고 현재 assembly/dependency·source/sample/importer 경로·기존 소비 도구를 읽어 [배포 규격](../../DISTRIBUTION_PIPELINE.md)을 작성했다. 사용자 요청으로 자체 구현 MIT와 [제3자 고지](../../../THIRD_PARTY_NOTICES.md), public 전환·외부 제공을 반영한다. 기존 사용자 변경은 [보호 입력](preserved-inputs.json)으로 확인한다.

[공개 점검](public-preflight.json)은 현재 reachable refs의 이력63 commits/1483 blobs, 알려진 credential pattern8개, scanner self-check3개를 다룬다. 검출0이며 원문 비밀값을 출력하지 않는다. 모든 가능한 credential 형식을 다루는 보장으로 표현하지 않는다. root LICENSE와 제3자 원문 조건을 대조하고 DLL/원문 license bytes를 유지한다.

[정적 결과](static-checks.json)는 source 입력317개 hash 불변·보호 파일6개 bytes 유지·문서110개/로컬 링크1141개·MIT 원문 일치·DLL hash·허용 stage 범위를 기록한다. 공백 검사도 통과했다. Unity 테스트·새 compile/build·artifact 설치 실행은 모두 **0건**이다. Runtime 동작 변경이 없어 문서 작업에 Unity 실행 테스트 대상이 없다. 과거 Edit/Play와 source-copy consumer 통과는 배포물 통과로 대체하지 않는다.

main 검증/반영 뒤 사용자가 승인한 저장소 public 전환을 GitHub에서 수행하고 실제 서버 상태와 비인증 조회를 확인한다. `v0.0.1` tag·Release와 패키지는 이번 설계 작업에서 생성하지 않는다. 수행한 commit·서버 public 상태는 마감 기록과 Git 이력을 따른다. 배포 자동화 구현과 실제 검증은 P1 이후다.
