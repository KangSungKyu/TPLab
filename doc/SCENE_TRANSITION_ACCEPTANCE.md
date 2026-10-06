# GameSceneManager 최종 수락 기준

이 문서는 Phase 6 후보에 대한 최종 사용자 확인을 한 번의 묶음으로 진행하기 위한 기준이다. Phase별 자동 검증과 코드 리뷰는 이 gate의 대체물이 아니다. 현재 전환용 sample scene, 메뉴, 실행 경로는 아직 확정·구현되지 않았으므로 아래 실행 경로는 **P6에서 실제 산출물을 확인한 뒤 기록할 항목**이다. 실행하지 않은 항목은 통과로 표시하지 않는다.

## 자동 증거 선행 조건

최종 사용자 확인을 요청하기 전에 부모가 아래 자동 증거를 현재 track tip에서 확보하고, 정확한 commit과 연결해 기록한다.

| 증거 | 확인 기준 |
|---|---|
| Phase 단위 | 해당 Phase 필수 EditMode/PlayMode 검사, compile, 제품 Console, diff 리뷰와 회고 완료. 실행 수·실패·skip 및 미실행 항목 기록 |
| Player | 지원 대상과 실제 Build 설정을 기록하고 최소 통합 경로 Player build 성공. 빌드 미실행·지원 여부 미확인은 성공으로 취급하지 않음 |
| 소비자 프로젝트 | 별도 소비 프로젝트에 공용 코어를 가져와 게임별 코드 없는 최소 통합 예제의 import/compile 및 필요한 실행 확인. MyLab 단독 테스트와 구분 |
| 씬·수명 통합 | Bootstrap→주 흐름→파생 구역 add/remove와 오류 정리의 자동 회귀, 예상/실제 loaded scene 및 root 소유 일치 |
| 반복 Play | Domain Reload와 Scene Reload를 각각 켜고 끈 네 조합에서 반복 진입·종료, root/installer 중복 및 정리 횟수 확인 |
| 빌드·Editor gate | 실제 Player build scene 목록, 모드/영속 소유 모순, 잘못된 정의·조건·중복 ID 차단. compile 및 제품 Console 오류 0건 |

Windows/WebGL/Linux 등 지원 여부는 해당 MyLab Editor의 실제 BuildPipeline 결과로 적는다. 설치 폴더 유무만으로 미지원이라 추정하지 않는다. 해당 타깃의 Player 빌드는 별도 증거다.

## 최종 사용자 확인 묶음

자동 gate가 끝난 뒤 하나의 확인 요청에서 각 시나리오의 실제 실행 절차와 기대 결과를 제시한다. 버튼·메뉴·scene path가 구현되기 전에는 경로를 만들어 내지 말고 P6 산출물을 기준으로 기입한다.

| 시나리오 | 직접 확인할 기대 결과 | 현재 상태 |
|---|---|---|
| Bootstrap 유지 + Additive 진입/주 흐름 교체 | Bootstrap과 공용 root가 유지되고 게임 root만 준비·교체된다. 전환 완료 전 입력이 차단되고 성공 후 복구된다. | 실행 경로·시각 확인 P6 대기 |
| 영속 공용 root + Single | 공용 root, manager, callback, cover 및 입력 소유자가 살아남는다. 이전 게임 subtree 정리 후 새 primary가 준비된다. | 실행 경로·시각 확인 P6 대기 |
| Hub↔Main 왕복 | 정의한 두 방향이 반복 가능하고 primary·active scene·입력 대상이 명세대로 바뀐다. 비활성/중복 씬이 남지 않는다. | 실제 sample 경로 P6에서 기록 |
| 중첩 파생 구역 | 부모를 유지한 채 자식 구역을 추가한다. 자식의 자기 제거와 부모의 subtree 제거가 자식 우선으로 끝나며, 부모 제거 시 하위 잔여 씬이 없다. | 실제 sample 경로 P6에서 기록 |
| 조건 거부와 설정 오류 | 거짓 조건은 씬 로드·cover·shutdown·OnFailure 없이 거부된다. 필수 조건/evaluator 누락과 권한 밖 제거는 사용자에게 진단되고 소유 상태가 바뀌지 않는다. | 자동 negative 검증 후 대표 오류 UI/로그를 확인 |
| 비동기 cover·입력·실패 | 비동기 준비/전환 동안 포인터와 키보드·게임패드 입력이 차단된다. 성공 reveal 완료 뒤 복원되고 실패·취소는 cover를 유지하며 잔여 root/scene 진단과 오류 표현을 보인다. | 실제 UI 표현·접근성은 P6에서 확인 |
| 반복 Play 조합 | 위 네 reload 조합에서 반복 Play 후 Singleton 중복, installer 중복 설치, 누락 복구, 남은 씬·가림막·입력 잠금이 없다. | 자동 반복 실행 증거와 사용자 최종 화면 확인 모두 필요 |

각 행은 `절차 / 기대 결과 / 실제 결과 / 통과·실패·미실행 / 증거`로 기록한다. 화면·사용성 확인과 자동 테스트·Player build·소비자 프로젝트 검증을 같은 결과로 합치지 않는다. UI 메뉴나 scene path가 아직 정해지지 않은 시나리오는 사용자에게 실행 가능한 것처럼 안내하지 않는다.

## 마지막 gate와 보존

최종 diff 검토에는 `main...codex/game-scenes-track` 변경을 사용하고, 사용자 소유의 기존 Unity 변경 4개는 track 산출물·승인 변경에 섞지 않는다. 비교 결과와 사용자 변경은 별도로 보존한다.

최종 확인 요청은 모든 Phase 자동 gate와 P6 증거가 끝난 뒤 한 번에 보낸다. 명시적인 사용자 답변만 이 gate를 해소한다. 무응답·시간 경과는 승인으로 간주하지 않는다. 확인 대기 중에는 main 병합/푸시와 track·Phase 브랜치 삭제를 보류한다. 자동 작업이 끝났고 최종 사용자 확인만 남은 경우 결과를 보존하고 Unity를 저장하되 Editor를 연 채 절전할 수 있다. 구현·검증 중에는 절전하지 않는다. 확인 후 main 통합 및 보존·정리가 모두 끝난 경우에만 Unity 정상 종료와 PC 종료 절차를 따른다.
