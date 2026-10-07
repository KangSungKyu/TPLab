# 2026-10-07-18 배포 실제 소비 검증

상태: 진행. Track `23e2764993e46d3fcd18e8d46d8029ac1863f1df`에서 phase `codex/dev-build-v0.0.1-p2-consumer`를 생성했다. P2는 실제 Git URL/tarball resolve·compile·Windows Mono 최소 실행, Core/Input 예제 분류·import 경로, Editor importer 두 launch 검증을 담당한다. main/tag/Release는 P3/P4의 gate다.

예제 경로 Red는 독립 fixture Unity에서 11/11 실패, 수정 후 Green은 12/12 통과했다. 이는 P1 Core/Input tarball에 새 sample 소스를 별도 fixture로 구성한 경로 계약 증거이며 P2의 실제 Sample.Import 증거와 구분한다. 이동한 기존 25개 meta blob/GUID는 보존했다. Core-only 예제와 자체 InputAction source를 추가했다. importer 생성과 컴파일 검증을 나눠 AwaitingCompilation을 통과로 오인하지 않는다.

부모가 source/tool/harness diff와 API를 리뷰했으며 Python 소비 계약은 최초 8개 Red(error6, skip1), 현재 11개 중 10개 통과·OS symlink 권한으로 1개 skip다. 패키징 sample 계약은 19개 중 최초1개 오류, 수정 후19개 통과다. 실제8개 소비 configuration와 원본 보호는 다음 기록에서 결과를 확정한다. [현재 증거](../validation/distribution-consumer/)와 [트랙](../DISTRIBUTION_PIPELINE.md)을 기준으로 인계한다.

## 마감 (2026-10-07)

상태: P2 자동 검증 완료 / track 통합. 검증 source `5f2e08d8bacb2320ca0832120f194c326869ce40`에서 Git/tarball8/8, 총30process(Editor18/기본 Player8/sample Player4) 통과, 빌드 오류0, sample48 checks다. 개발 경로 fixture는4process·two-mode24 checks로 source 위치와 import 위치를 각각 확인했다. 최종 Python33 methods32pass/1skip(native symlink OS 권한), Unity path12/12다. 원본 main/Editor/사용자6파일과 기존25metaGUID를 보존했다.

실제 소비는 source-copy 증거로 대체할 수 없었다. Alias 이름 충돌, 설치된 manifest에 UPM만 추가하는 fingerprint32/40 차이, Unity가 batch 종료/다음 build에 Temp output을 정리하는 수명 문제를 각각 관찰·기록·수정했다. DLL gate 리뷰에서 발견한 잘못된 비교 조건은 actual failing regression으로 수정했다. generation 성공을 typed validation 성공으로 표시하지 않고 별도 launch에서 실제 compiled type/validator를 확인했다. source/archive payload는 유지하고 검증 harness의 오류를 좁게 수정했으며 새 후보/source/output에서 최초 실패를 보존해 재검증했다.

사람/AI 진입 문서를 실제 후보 설치와 sample opt-in 절차로 갱신했다. packager 생성 시 NotRun 상태와 이후 소비 통과, 원본 Editor 확인과 독립 batch fixture, physical/visual 사용자 수락을 구분한다. 상세 수치/해시는 [현재 증거](../validation/distribution-consumer/README.md)에 둔다.

P2 phase 원본 commit을 보존해 track으로 반영·push한다. main/tag/Release·branch 삭제는 대기한다. P3는 fixed 후보 회귀·문서/정책 gate 및 새 설치/sample workflow의 최종 한 번 사용자 확인, P4는 main/tag/Release/실제 내려받은 첨부물/tag URL을 확인한다. 다른 Unity/PC/플랫폼/IL2CPP는 이번 결과로 보장하지 않는다. 필요한 후보/full consumer/canonical fixture는 P3에 보존하고, 나머지는 증거 보존 후 명시 목록으로 정리한다.
