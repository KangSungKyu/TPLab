# 2026-10-07 · Input 통합 검증과 수락 대기

- 기준: track `3b0adaa`, 작업 `codex/input-system-p4-validation`. [앞 단계](2026-10-07-06-input-integration.md)의 runtime을 전체 기존 코어·실제 consumer/Player와 연결해 검증했다.
- 변경: 기존 reload harness에 선택적 input scope 관찰을 추가하고 입력 포함/제외 consumer allowlist·최소 New Input 설정을 제공했다. 기존 Player builder의 증거 경로를 입력 검증 폴더로 확장했다. compiler 오류가 있으면 test runner가 실행 전 거부한다. 별도 backend·저장 UI·새 로딩 화면은 추가하지 않았다.
- 리뷰/TDD: 하위 에이전트의 읽기 전용 리뷰에서 초기 BlockAll의 native canceled callback이 owner를 종료할 때 ODE가 전달됨을 발견했다. 실제 Red1/실패1 후 취소된 linked token의 ODE만 OCE로 전달했다. disposal·graceful 종료를 포함한 rebind Green11/11을 확인했다. 일반/정리 오류를 취소로 숨기지 않는다.
- 최종 실행: 같은 PID23120의 Edit258/258·Play217/217, 실패0/skip0. 네 reload 조합×2, Additive/Single WindowsMono build·Player 각10관찰, Input 제외 consumer11개/포함 consumer12개 관찰과 각2실행 통과. 원본 asset·준비 차단·clone 종료·중첩 lease·UI Submit 재진입을 확인했다. 상세 증거와 보완 전 이력은 [검증 기록](../validation/input-system/p4/README.md)에 있다.
- 보존: 사용자 dirty4와 보호7 raw bytes를 유지했다. 테스트 refresh의 Build Settings missing GUID 정규화와 빌드가 만든 clean 설정4개만 원래 검증된 bytes로 복원했다. 기대한 테스트 오류는 보존하고 정상 재진입 이후 새 Console을 별도로 확인했다. 임시 출력은 durable 결과·manifest/lock·Player log·진단 요약을 보존한 뒤 이번 소유 경로만 정리한다.
- 한계: WindowsMono/Unity6000.3.18f1 한 환경, 가상 Keyboard·native UI module 검증이다. 물리 장치·포인터/touch·시각 UX·다른 Unity/platform·InputUser/파일 저장·강제 double rollback 실패는 미검증 또는 제외다. 소비 build template의 구형 backend API compiler warning과 license 갱신 진단을 BuildReport warning0으로 숨기지 않았다.
- Git: 단계4를 원본 commit 보존해 track에 통합·push하고 정확한 source commit의 CI 정책을 확인한다. CI 미구성은 CI 성공이 아니다. main은 사용자 확인 대기이며 branch 삭제·PC 종료/절전은 수행하지 않는다.
- 다음: [최종 수락 절차](../INPUT_SYSTEM_ACCEPTANCE.md)로 키 설정·메모리 저장/reset/restore·modal·Single/Additive·실제 장치 결과를 한 번에 확인한다. 명시적 확인과 필요한 자동 gate가 끝나면 최신 main과 통합 후 승인된 작업 브랜치만 정리한다. 진행률/팁·자동/버튼 진행 UI는 별도 설계 단위다.
