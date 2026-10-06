# Phase 6 통합 실행 검증

2026-10-06. 기준 track `de4bb691aed3124443c4bfe3cfd085e3275002a8`, 작업 `codex/game-scenes-p6-validation`. 자동 gate 완료·사용자 확인 대기다. main 병합·브랜치 삭제는 보류한다. 기존 MyLab Editor PID23176 / Unity6000.3.18f1 / Connector0.4.1이 내부 검증을 소유한다.

## 반복 Play

[실제 반복 진입](reload-observed.json)은 Domain/Scene Reload 네 조합×두 번, attempted8/completed8/success=true이다. common/game 준비와 새로운 manager, Configure/Presentation/Reveal 및 graceful manager→common 해제·unload를 관찰했다. 첫 combo에만 fixture를 다시 열고 두 번째에는 Configure/reset/reopen을 하지 않았다. 옵션·startScene·씬 setup·build list/raw bytes를 복원하고 owned fixture를 제거했다. 보호5 raw bytes도 baseline과 일치했다.

[첫 fixture 오류](fixture-failure-reload.json)는 None에서 legacy enabled getter=false를 거부한 검사 가정이었다(1attempt/0complete). [두 번째 fixture 오류](identity-fixture-failure-reload.json)는 Both-disabled에서도 C# 래퍼의 ReferenceEquals가 같아야 한다는 잘못된 가정이었다(8attempt/7complete). 동일 native ID와 실제 옵션은 관찰됐지만 Unity는 새 C# 래퍼를 제공했다. 이를 Bootstrap 결함의 Red라고 주장하지 않는다. 최종 관찰의 entry7은 이전 manager가 Stopped이고 새 Bootstrap.Manager=null/_stopping=false에서 출발한 뒤 새 manager로 성공했다. 예상한 상태 잔류 결함이 재현되지 않아 제품 Bootstrap reset을 추가하지 않았다. 직접 호출 autoStart=false + graceful 종료를 검증한 범위이며 abrupt Play stop/늦은 이전 작업/모든 자동 시작 경로를 입증하지 않는다.

## 소비 프로젝트

[첫 actual consumer run](core-consumer-20261006T123529Z-10048.json)은 별도 batch Editor PID39508에서 같은6000.3.18f1의 WindowsMono build 성공(errors0/warnings0/exit0)을 관찰했다. driver는 Unity가 별도 -logFile로 기록한 실제 로그 대신 빈 stdout을 요구하여 gate에서 실패했다. Player 실행0이며 전체 소비 성공으로 표시하지 않는다. 실제 log의 freshness를 사용하는 도구 보완 후 새 owned 출력에서 재검증했다.

[두 번째 actual consumer run](core-consumer-20261006T123935Z-47452.json)은 success=true/actualRuns2이다. 같은 Unity6000.3.18f1의 별도 batch Editor PID47652에서 WindowsStandalone64 Mono build(errors0/warnings0/exit0), Player PID44252에서 exit0 및 11개 관찰 항목을 확인했다. common/root 준비, 최초 Additive/primary active, 파생 add/remove, 관리 pool 재사용, typed CSV 조회와 graceful 종료를 실행했다. ResourceManager는 빈 manager 종료만 실행했으며 실제 Addressables 자산 로드의 증거로 확대하지 않는다. 실제 fresh 로그와 빌드·Player 결과, manifest/packages-lock은 [영속 자료](CoreConsumer-P6-20261006-02/)에 보존했다. 코어와 승인 의존성만 allowlist로 가져왔으며 MyLab Editor·테스트·UI·게임별 코드는 복사하지 않았다.

통합 UI sample·원래 Editor 성공 Player build/run·전체 회귀·최종 Console/입력 hash를 확인했다. 사용자 시각·사용성 확인은 대기 중이다. main·브랜치 삭제·절전은 자동 gate와 인간 확인 정책에 따른다.

소비 도구/자료 읽기 리뷰: allowlist 원본과 복사본86/86 및 template hash가 일치한다. BuildReport.totalWarnings=0은 compiler warning0을 의미하지 않는다. 실제 Editor log에 구형 PlayerSettings backend API CS0618 경고가 초기·빌드 compile에서 각2회 있고, 초기 license handshake/access-token 오류 뒤 entitlement 갱신/빌드 성공이 있다. 원본 로그를 보존하고 무경고 compile로 주장하지 않는다. Player log에서 검토한 warning/error 패턴은 없었다.

## 샘플 입력 계약 TDD

새 meta 두 개의33자리GUID로 [초기 접수](sample-discovery-zero.json)는 발견0건이었다. 새 owned meta만 정확32자리GUID로 수정하고 명시import했다. 설치 NUnit에서 Assert.Multiple 미지원 컴파일 오류 두 개를 개별 동일 assertion으로 교체했다. 오류·발견0은 Red/Green이 아니다. 실제 발견 후 [Red](sample-red-play.json) 2total/0passed/2failed/0skip(CLI43740), [최소 Green](sample-green-play.json) 2/2/0/0(CLI39876)을 원래 Editor PID23176에서 확인했다. canProceed와 독립 transition/modal 차단을 모두 만족할 때 Player map을 켠다. UI map은 독립 유지하고 borrowed input source를 변경하지 않는다. 최종 sample Green2/2 및 전체 Edit240/240·Play201/201를 확인했다.

GUID import/refresh 과정에서 기존 build list의 missing SampleScene 경로 GUID가 Unity에 의해0으로 재직렬화됐다. [정규화 내용](build-settings-refresh-drift.txt)을 보존하고, 원래 raw SHA256이 검증된 반복 Play snapshot에서 정확 bytes를 복원했다. SampleScene을 생성하거나 기존 자산 GUID를 바꾸지 않았다. 최종 보호5 raw bytes는 baseline과 일치하며 Player 설정의 작업 전 raw hash도 일치한다.

## 통합 sample과 native 실행

별도 Samples assembly에 Bootstrap Additive/Single, Hub/Main, Area/Nested 6씬·설정2개와 명시 생성 메뉴를 제공한다. 게임 UI/InputSystem 의존성은 sample에만 있다. Core/패키지는 P5 이후 변경하지 않았다. common root 설치→consumer 주입→async 준비→presentation 완료→reveal 계약을 실제 실행한다. input source는 clone하고 Player 권한과 transition/modal 차단을 독립 소유한다. ScreenSpaceOverlay order100/200/300과 완전 불투명 cover/modal을 사용한다.

[authoring 실패](sample-authoring-failure.txt) 2회는 NewScene이 기존 settings C# wrapper를 무효화한 문제였다. [원래 Editor 관찰](settings-new-scene-observed.json)에서 경로 재로드가 유효함을 확인해 최소 보완했다. 최종 생성/반복 생성의 [GUID8개](sample-asset-guids.json)를 보존했다. background delayCall의 pending restore를 update 1회로 바꾸고 [실제 정상 Play stop 복구](automatic-restore.json)를 확인했다. crash 복구를 주장하지 않는다.

[Additive Editor 최종 smoke](editor-additive-final-smoke.json)와 [Single Editor smoke](editor-single-smoke.json)는 각10항목이다. 최초 준비·Hub/Main 왕복·중첩 추가/자기 제거/ancestor 제거·조건 거부 무변경·독립 modal 유지·실패 cover 유지·graceful 종료를 관찰했다. Single 마지막 씬 unload를 위해 smoke harness가 소유한 빈 recovery scene 1개를 만들며 일반 UI의 자동 복구 정책은 아니다.

[실제 ready 화면](additive-ready.png), [최초 겹침 발견](additive-failure-held-after-shutdown.png), [불투명 보완 후 실제 화면](additive-failure-held-final.png)을 보존했다. parent가1600×900 화면을 확인했지만 인간 사용성·다른 화면비·물리 입력 확인을 대신하지 않는다.

원래 Editor의 WindowsStandalone64 Mono [Additive build](build-additive.json), [Single build](build-single.json)는 Succeeded/errors0/BuildReport.warnings0 및 복구 flags=true다. Additive sync CLI 응답은 timeout됐지만 동일 실행의 fresh native BuildReport가 성공했고 Player가 실제 실행됐다. 새 Editor나 다른 빌드로 대체하지 않았다. native log는 [보존 로그](additive-build-native.log)에 있다. BuildReport warning 수를 전체 compiler 무경고로 해석하지 않는다.

[Additive Player](scene-player-additive-20261006T134438Z-49780.json)(PID45936)와 [Single Player](scene-player-single-20261006T134658Z-49992.json)(PID50444)는10/10관찰·exit0·fresh log/result·Unity6000.3.18f1을 확인했다. runner의 정상/최적화 Python self-check도 통과했다. 실제 빌드가 만든 URP prefilter/runtime cache·PlayerSettings default·UnityConnect 변경은 [작업 전 복원](build-side-effect-restoration.json) 후 원래 Editor에서 재import/저장했고 사용자 변경은 보존했다.

## 최종 회귀와 남은 gate

[전체 Edit](full-EditMode.json)240/240/0/0, [전체 Play](full-PlayMode.json)201/201/0/0을 원래 PID23176에서 실행했다. Play 최초 접수 실패는 prestart discovery rejection이며 실제 테스트 실행은1회다. `cliReturnCode=1` 결과는 이번 PID/runId Connector 파일의 positive named result로 검증했으며0건을 통과로 취급하지 않는다. 의도된 실패경로 로그는 [보존 Console](expected-regression-console.json)에 있고, 예상 LogAssert와 통과된 결과에 대응한다. 이후 [Console0](final-console.json)와 [ready](final-status.txt), current 입력 hash·원래 보호 bytes·8GUID·문서 링크를 verifier로 확인한다. 새 해상도·플랫폼·다른 Unity 버전과 모든 Addressables 실제 콘텐츠 조합은 검증하지 않았다.

[최종 사용자 절차](../../SCENE_TRANSITION_ACCEPTANCE.md)에 Additive/Single·조건·중첩 해제·실패 가림막·modal/입력·16:9/4:3·원래 설정 복구를 한 번에 묶었다. 자동 gate와 인간 확인을 분리한다. track/Phase를 보존하고 main과 브랜치 삭제는 명시적 최종 확인까지 기다린다. 저장한 Unity Editor를 유지한 채 절전한다.
