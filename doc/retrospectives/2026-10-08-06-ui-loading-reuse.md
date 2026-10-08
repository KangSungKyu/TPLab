# UIContext P2 등록·준비·재사용

2026-10-08. 기준 codex/game-ui-p2-loading / 69b0826. P1의 검증된 track tip에서 새 branch를 생성했다. 목표는 direct/key provider 공통 준비, 선택적 자산 선행 준비와 runtime lazy 표시, 표시 세대별 재사용/실패 정리였다.

## 구현과 결정

소스6파일을 cea4268b82e0f119e0d4dbc8c788b7632d21f3ee로 기록했다. [Runtime](../../Assets/TPLab/UI/Runtime/UIContext.cs)은 key별 공유 completion과 definition별 clone 보관1개를 내부에 둔다. public PrepareAsync는 자산만 준비하고 clone을 생성하지 않는다. inactive clone의 데이터/구독 준비는 표시 경로의 UIHooks.PrepareAsync가 맡는다. Asset provider는 빌리며 native 자산 해제는 원 소유자의 책임이다.

caller의 준비 대기 취소가 공유 자산 로딩을 취소하지 않는다. owner 종료는 UI 대기를 취소하고 late source를 표시/cache에 반영하지 않는다. 실패 entry는 다음 명시 요청에서만 다시 만든다. 새 public Factory/Pool 계층을 추가하지 않았다. 설치 PrefabPool은 활성 clone을 반환하고 sync onRent로 async prepare를 기다릴 수 없으므로 여기서는 비활성 보관1개의 작은 경계를 사용한다.

reuse마다 새 handle/token/cleanup을 만든다. 준비/열기/cleanup 실패 clone은 폐기하며 Closed observer가 새 표시를 시작해도 이전 후보를 빌려주지 않는다. observer 성공 뒤 cache를 공개하고 실패/초과 후보만 파괴한다. private retiring handle은 owner 종료가 실제 폐기를 기다리도록 추적한다. Borrowed root/prefab/provider는 파괴하지 않는다.

## 문제·검증·한계

[실제 근거](../validation/ui-system/p2/README.md): 처음 Red Edit5/Play7 모두 실패·skip0, 이후 UI 전체 Edit14/14+Play16/16 실패0·skip0, compile/제품 Console 오류0, 보호6파일 unchanged. 기존 P1 18개와 새12개를 포함한30개다. 최초 Green 연결 시간 초과는 결과 없는 invocation으로 보존하고, 동일 Editor의 실제 연결 복구/테스트 idle 확인 후 새 결과를 얻었다. 과거 결과로 대체하지 않았다.

문서 초안에서 자산 Prepare와 display hook 준비를 혼동한 표현을 찾아 수정했다. actual source/XML과 사람/AI 문서를 같은 단위에서 대조한다. after-await 자기 lifecycle 순환 대기 자동 검출의 기존 한계는 유지한다. 소비/Player/Profiler/사용자 UX는 P7 미실행이다.

소스 commit 후 문서/증거/회고를 별도 commit으로 보존하고 track에 fast-forward 통합한다. 정확한 최종 SHA는 Git 이력에서 확인한다. main8ce768d·upm/version/tag/Release는 변경하지 않으며 최종 사용자 수락 전에 main 병합/branch 삭제를 하지 않는다. 다음 P3는 실제 Canvas host·HUD/parent/순서·명시 숨김으로 진행한다. 임시 문서 초안2개는 최종 문서 반영 후 지정 경로만 제거한다.
