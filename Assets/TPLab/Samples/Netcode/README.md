# Netcode session sample
유형: 레퍼런스
현재 상태: P5 예제 자동 검증 완료; 사용자 수락 대기
기준 source: PR #39 병합 main `3a4216b90e6f6b21a26c186661bf3dbce4221e6e`의 wrapper; 예제 검증 source는 회고 참조
갱신일: 2026-10-16
계약 링크: [Netcode API](../../../../doc/api/Netcode.md)

## 목적과 범위

하나의 고정 씬에서 Host와 두 Client가 프로젝트 준비 callback을 완료하고, 서버의 시작 승인을 받은 뒤에만 로컬 gameplay UI를 여는 예제다. 프로젝트 소유 로비·가림막·준비·release·복원 callback을 wrapper에 연결하는 위치를 보여준다. 실제 인증, 매칭, Relay, 게임 오브젝트 복제, 팀 구성과 공유 씬 전환은 포함하지 않는다. 예제의 Interact 카운터는 각 Player의 로컬 관찰값이며 동기화된 게임 상태가 아니다.

개발 소스와 NGO 2.13.3/Transport 2.7.3, UniTask, uGUI, Input System이 필요하다. TPLab Netcode runtime을 별도로 가져와야 하며 아직 배포 UPM에 포함된 예제가 아니다. Core/UI/Input 기존 assembly나 GameSceneManager를 변경하지 않는다.

## 실행 준비

1. `Scenes/NetcodeSessionSample.unity`를 연다. 저장된 씬·입력 자산이 이미 포함되어 있으므로 생성 메뉴를 다시 실행할 필요가 없다.
2. 실제 실행/빌드 전 이 씬을 Build Settings의 활성 씬 목록에 포함한다. 예제 생성기는 사용자 Build Settings를 수정하지 않는다. 기존 프로젝트 설정 변경은 사용자가 관리한다.
3. Windows Standalone 빌드에는 이 씬을 첫 씬으로 지정한다. 동일한 빌드를 세 번 실행하거나, Editor 한 개와 같은 씬의 Player 두 개를 사용한다. 다른 씬에서 실행한 Client를 이 데모와 혼용하지 않는다.
4. 세 창의 Session/Attempt/Port를 동일하게 맞춘다. 기본 예제 값은 `tplab-demo`, `round-1`, `7777`이다. Host의 Roster는 `host,c1,c2`; 각 창의 Role은 Host/Client1/Client2로 지정한다.
5. Host에서 Start / Join을 먼저 누른 후 두 Client에서 누른다. 로컬 `Preparing`이 표시된 창에서 Ready를 누른다. 마지막 한 명이 Ready하기 전에는 모두 gameplay가 차단돼야 한다.

씬의 NetworkManager는 별도 최상위 오브젝트다. `EnableSceneManagement=true`가 권장 설정이며 예제 Host의 초기 동기화 모드는 Additive다. 이 씬은 이미 각 Player에 로드돼 있어야 하며 예제 owner를 씬 전환으로 교체하지 않는다. `false`는 실행 전에 프로젝트가 Inspector에서 선택할 수 있지만, 동일한 씬 준비는 프로젝트 책임이다. 게임 중 공유 씬 전환과 GameSceneManager의 소유권 통합은 후속 범위다.

`TPLab/Netcode/Create Session Sample` 메뉴는 코드만 가져온 경우 **새** 씬·입력 자산을 생성하는 authoring 경로다. 대상 파일이나 `.meta`가 이미 있으면 덮어쓰기 전에 거부한다. 기존 씬, 공유 입력 자산, Build Settings를 저장하지 않는다.

## 조작과 실패 확인

| 조작 | 확인할 동작 |
|---|---|
| Start / Join | 새 Context로 새 시도 시작. 활성 시도·정리 중 중복 시작을 거부한다. |
| Ready | 로컬 준비 완료. 서버가 확정한 모든 참가자의 준비가 끝날 때까지 보호 화면을 유지한다. |
| Fail Prepare | 로컬 준비 예외를 발생시킨다. 정리 후 로비에 정규화 실패 사유를 표시하며 gameplay는 시작하지 않는다. |
| Stop | caller 대기만 취소하는 기능이 아닌, 소유한 세션의 종료다. native 종료와 callback 정리를 기다린다. |
| Fail first lobby restore | 시작 전 선택한다. 이어 Fail Prepare로 복원 경로에 들어가면 한 번 실패하고 RecoveryFault·보호 화면을 유지한다. |
| Retry Restore | 실제 정리 완료 후에만 활성화된다. 같은 실패·시도 식별자로 복원만 재시도한다. |
| Interact | 시작 승인 후에만 로컬 카운터가 증가한다. 보호 화면의 raycast와 controller의 gameplay 조건을 함께 사용한다. |
| Quit Safely | 실제 종료 완료 뒤 Player를 종료한다. Editor에서는 Stop 완료를 확인한 후 직접 Play Mode를 종료한다. |

준비 중 누군가 Stop하면 실패·이탈 사유를 표시하고 살아 있는 참가자는 로비 복원을 수행한다. 이미 Running인 상태의 종료는 자동 복원 callback 경로와 다르다. 예제는 Closed를 확인한 뒤 로비 UI를 표시한다. Running 중 연결을 잃으면 입력을 막고 명시적인 Stop을 안내한다.

재시작에는 세 창이 **새로운 공통 Attempt 값**을 사용한다. 같은 Session/Attempt 재사용은 이 component의 수명 동안 거부한다. 이전 Context를 재사용하지 않는다. 복원 재시도는 새 시도가 아니므로 원래 key를 유지한다.

## 프로젝트에서 바꿀 부분

- `NetcodeSessionSample`은 프로젝트 callback의 예다. Prepare의 수동 gate를 실제 자산/씬 root 준비로 바꿀 때 owner 취소를 협조적으로 처리하고 release 완료를 기다린다.
- `NetcodeSessionSampleView`는 uGUI/Input System 화면을 연결한다. gameplay Canvas는 order 0, 전체 보호막 50, Ready/Stop 등 세션 조작 UI 100이다. 세션 조작은 보호막 위에서 유지한다.
- demo-host/demo-c1/demo-c2는 loopback 검증용 고정 문자열이다. 누구나 만들 수 있으므로 인증·ticket·replay 방어가 아니다. 실제 프로젝트는 발급·검증·재사용 방지를 담당하는 검증기를 주입한다. 이 예제는 127.0.0.1로만 연결/수신한다.
- 전체 envelope 4,096bytes, 준비 120초, 종료/복원 각각 10초는 **예제에서 명시한 값**이다. 공용 wrapper 기본값이나 운영 권장 수치가 아니다.
- `Configure`는 manager를 빌린다. 정상 종료에서 `ShutdownAsync`를 await한 뒤 owner/manager를 파괴한다. OnDestroy는 비동기 종료를 기다릴 수 없는 fallback이며 정상 복원 완료를 보장하지 않는다. 미시작 Shutdown 결과는 null이다.
- public caller cancellation은 해당 대기만 취소한다. 실제 owner 종료는 ShutdownAsync다. 보호 화면을 해제하기 위해 timeout 결과를 성공으로 취급하지 않는다.

## 사용자 수락 목록

자동 검증과 별도로 아래 실제 조작을 확인한다. 모든 항목이 확인되기 전에는 사용자 수락 완료로 기록하지 않는다.

| 항목 | 직접 확인 |
|---|---|
| 3개 창 정상 시작 | Host/Client 역할과 텍스트 입력·드롭다운이 작동하며 마지막 Ready 전 gameplay가 열리지 않는다. |
| 준비 실패·이탈 | Client Fail Prepare 또는 준비 중 Stop에서 살아 있는 창의 실패 사유·로비 복원을 확인한다. |
| 복원 실패·재시도 | Host에 Fail first lobby restore를 켜고 실패시킨다. RecoveryFault에서 보호막 유지, Retry 후 로비 복원을 확인한다. |
| 새 시도 | 모두 Stop 후 새 공통 Attempt로 재시작한다. 이전 시도 결과나 Ready가 새 시도에 섞이지 않는다. |
| 화면·입력 | 창 크기/화면비를 바꾸고 초점을 다른 창에 옮겼다 돌아온다. 글자 겹침·잘림과 보호 중 gameplay 입력 누출이 없는지 확인한다. |
| 정상 종료 | 미시작 상태 및 세션 종료 후 Quit Safely가 닫히는지 확인한다. OS 강제 종료를 정상 cleanup 증거로 사용하지 않는다. |

자동 실행 조건·실패 이력·미실행 경계는 [P5 회고](../../../../doc/retrospectives/2026-10-16-05-netcode-p5-sample.md)에 기록한다. 사용자 수락은 대기 상태다.
