# 2026-10-07-18 배포 실제 소비 검증

상태: 진행. Track `23e2764993e46d3fcd18e8d46d8029ac1863f1df`에서 phase `codex/dev-build-v0.0.1-p2-consumer`를 생성했다. P2는 실제 Git URL/tarball resolve·compile·Windows Mono 최소 실행, Core/Input 예제 분류·import 경로, Editor importer 두 launch 검증을 담당한다. main/tag/Release는 P3/P4의 gate다.

예제 경로 Red는 독립 fixture Unity에서 11/11 실패, 수정 후 Green은 12/12 통과했다. 이는 P1 Core/Input tarball에 새 sample 소스를 별도 fixture로 구성한 경로 계약 증거이며 P2의 실제 Sample.Import 증거와 구분한다. 이동한 기존 25개 meta blob/GUID는 보존했다. Core-only 예제와 자체 InputAction source를 추가했다. importer 생성과 컴파일 검증을 나눠 AwaitingCompilation을 통과로 오인하지 않는다.

부모가 source/tool/harness diff와 API를 리뷰했으며 Python 소비 계약은 최초 8개 Red(error6, skip1), 현재 11개 중 10개 통과·OS symlink 권한으로 1개 skip다. 패키징 sample 계약은 19개 중 최초1개 오류, 수정 후19개 통과다. 실제8개 소비 configuration와 원본 보호는 다음 기록에서 결과를 확정한다. [현재 증거](../validation/distribution-consumer/)와 [트랙](../DISTRIBUTION_PIPELINE.md)을 기준으로 인계한다.
