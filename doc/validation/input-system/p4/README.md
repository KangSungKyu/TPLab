# Input wrapper 최종 자동 검증

2026-10-07. 기준 `3b0adaa`, 작업 `codex/input-system-p4-validation`. 원래 MyLab Editor PID23120, Unity6000.3.18f1/InputSystem1.19.0/Connector0.4.1에서 실행했다. 자동 gate와 [실제 사용자 확인](../../../INPUT_SYSTEM_ACCEPTANCE.md)을 구분한다. 입력 최종 사용자 수락은 완료됐다. 이후 문서 작업에서 runtime 입력304개와 보호7개를 대조했으며 새로운 Unity 실행은0건이다. [최종 통합](final-integration/README.md)을 따른다.

## 최종 실행

| 검증 | 실제 결과 | 증거 |
|---|---|---|
| 전체 EditMode | 258/258, 실패0·skip0 | [최종 Edit](full-EditMode.json) |
| 전체 PlayMode | 217/217, 실패0·skip0 | [최종 Play](full-PlayMode.json) |
| Domain/Scene Reload 네 조합×두 진입 | attempted8/completed8, 입력 scope·clone·준비 차단·lease 복원·종료·설정 복구 | [최종 재진입](reload-observed.json) |
| 실제 Additive/Single sample Windows Mono | 두 build Succeeded/errors0/BuildReport warnings0, 두 Player 각10관찰·exit0 | [Additive build](build-additive.json), [Single build](build-single.json), [Additive Player](scene-player-additive-20261007T033441Z-25664.json), [Single Player](scene-player-single-20261007T033537Z-30492.json) |
| Input 제외 소비 프로젝트 | 별도 batch Editor build와 Player 총2실행, 기존11개 관찰 통과 | [제외 consumer](consumer-excluded/core-consumer-20261007T031416Z-28236.json) |
| Input 포함 소비 프로젝트 | 마지막 runtime 보완 포함, 별도 batch Editor build와 Player 총2실행, 기존11개+inputScopeVerified 통과 | [최종 포함 consumer](consumer-included-final/core-consumer-20261007T033305Z-23128.json) |

전체 Unity tests는 각각 한 번의 실제 최종 실행이며 정확한 project/Editor/CLI PID와 named passes를 결과에 남겼다. 소비 프로젝트의 별도 Editor는 공용 모듈 가져오기 검증용이며 MyLab의 원래 Editor 테스트를 대신하지 않는다. Windows Mono 외 플랫폼·IL2CPP·다른 Unity 버전은 미실행이다.

마지막 reload 재실행의 첫 종료에서 Editor가 background/프레임2에 멈춰 있었다. 현재 Play session의 `Application.runInBackground`만 true로 설정해 frame을 진행했고 최종8/8을 확인했다. Exit 이후 값은 다시 false였고 ProjectSettings raw bytes도 유지됐다. 검증 중 source reset이나 Bootstrap 재설정으로 결과를 대체하지 않았다.

## 리뷰와 TDD 보완

독립 읽기 전용 리뷰에서 native canceled callback이 리바인딩의 초기 BlockAll 중 owner를 종료하면 ODE가 전달되는 결함을 찾았다. [실제 Red](review-red-play.json) 1실행/1실패는 해당 ODE를 재현한다. 취소된 linked token이 있는 ODE만 OCE로 전달하고 원래 예외를 inner로 보존했다. 일반 오류와 정리 오류는 그대로 전달한다. disposal·graceful shutdown 재진입을 포함한 [리바인딩 Green](review-green-play.json) 11/11, 실패0·skip0을 확인했다.

[보완 전 Edit](before-review-EditMode.json)258/258·[Play](before-review-PlayMode.json)215/215·[reload](before-review-reload.json)8/8과 [초기 포함 consumer](consumer-included/core-consumer-20261007T031939Z-19668.json)는 이력이다. 마지막 변경의 완료 근거는 위 최종 결과다. 제외 consumer의 allowlist는 변경하지 않은 Core이며 Input 코드를 복사하지 않는다. 이번 검증 도구에는 compiler 오류가 남아 있으면 테스트를 시작하지 않는 guard와 self-check를 추가했다.

## 관찰 범위와 한계

sample Player는 최초 진입·주 씬 왕복·중첩 추가·자기/ancestor 해제·조건 거부 무변경·modal이 reveal 뒤 gameplay를 계속 막음·실패 cover 유지·graceful 종료를 실행했다. 이 smoke는 현재 Map 활성 상태를 관찰하며 물리 키·포인터·렌더링 UX를 입증하지 않는다. 입력 소비 Player는 가상 Keyboard 후보 선택→버튼 release→적용, 원본 보존, 독립 modal/lease 복원, JSON/reset과 clone 종료를 실행했다.

전체 입력 테스트는 Edit18/Play16이다. 같은 frame의 Submit이 새 focus에 중복 전달되지 않는 native UI module 테스트를 포함한다. 물리 게임패드·mouse/touch·최종 설정 UI, 파일 저장, InputUser 멀티플레이, 의미상 wildcard/alias 충돌은 이번 자동 증거 범위 밖이다. JSON native commit과 rollback이 연속 실패하는 fault 경로는 구현됐지만 강제 실패 실행은 하지 않았다. reload는 저장된 fixture의 명시 BootstrapAsync와 graceful 종료이며 abrupt Play stop의 모든 늦은 작업 조합을 보장하지 않는다.

## Console·보존·도구

[예상 실패 경로 Console](expected-regression-console.json)은 기존 LogAssert 테스트와 통과 결과에 대응한다. 로그 보존 후 clear하고 정상 재진입에서 [최종 오류 Console](final-console.json)과 [원래 Editor 상태](final-status.txt)를 확인했다. compiler/제품 Console와 테스트 성공을 구분한다.

소비 Editor의 BuildReport warnings0은 전체 compiler 무경고를 의미하지 않는다. 기존 소비 build template의 CS0618 두 종류가 초기/Player compile에서 반복된다. license handshake/access-token 갱신 오류 뒤 실제 build/Player가 성공했다. 이 진단은 consumer 자료의 요약으로 보존하며 인증 정보가 섞일 수 있는 Editor 원문은 저장소에 복사하지 않는다. Player log·실제 result·manifest/lock은 각 consumer 증거 폴더에 보존한다.

빌드가 변경한 네 파일만 [관찰·복원 기록](build-side-effect-restoration.json)에 따라 원래 clean HEAD bytes로 복원했다. 테스트 refresh의 missing SampleScene GUID 정규화는 이번 reload backup의 검증된 원래 Build Settings bytes로 복원했다. 기본 입력 asset·ProjectSettings·사용자 변경을 포함한 [보호7 raw hash](preserved-inputs.json)와 [최종 소스 입력](test-inputs.json)을 대조한다. `checks.json`과 재사용 `tools/verify_validation.py`는 저장한 건수·현재 source/보호 bytes·GUID·문서 링크·최종 Editor 증거를 확인하며 새 테스트를 실행하지 않는다.

도구 self-check: test runner의 파서/컴파일 거부, consumer allowlist·포함/제외 gate, Player 결과의 nonzero exact count·mode/version·오류 gate(일반/최적화 Python), hash 정규화를 실행했다. 실제 GitHub CI 정책은 push한 정확한 source commit을 조회한 별도 자료로 기록한다. CI가 미구성이면 이를 CI 성공으로 표현하지 않는다.

source `9305b5dd0f730636f431fd5d19a1c9102fdc3bed`를 track에 fast-forward 통합·push했다. [원격 source 정책](source-ci-policy.json)은 workflow/check/status/run 모두0, main 무보호·CI 미구성이다. main tip은 기준 `ce290878da51c653eaa219aecc45e6bf61ac755a`로 유지했다. 사용자 dirty4만 남고 현재 source 입력304개·보호7개는 verifier를 통과했다. 후속 정책·수락 기록 commit은 source/test 변경이 아니다.

재생성 가능한 consumer/Player 출력과 harness 백업은 필요한 영속 자료를 보존하고 실행 종료·원래 bytes 복구를 확인한 뒤 이번 소유 경로만 정리한다. 인간 확인은 기존 Editor sample에서 수행하며 임시 Player 경로에 의존하지 않는다.

소스·문서·JSON 공백 검사는 통과했다. native Player `.log` 원문에는 Unity가 기록한 trailing space가 있어 공백 검사에서 로그만 제외하고 bytes를 보존했다.
