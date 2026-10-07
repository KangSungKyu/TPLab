# 재사용 검증 도구

작업별 입력과 출력은 명시한다. 과거 출력은 현재 테스트/승인을 대신하지 않는다. Python 표준 라이브러리만 사용하며 도구는 파일 삭제·Git 통합을 수행하지 않는다. run_unity_tests.py만 명시한 기존 Unity Editor에 테스트를 요청한다.

```powershell
python tools/verify_validation.py --evidence doc/validation/scene-transition-contracts
python tools/check_github_ci.py --repository KangSungKyu/TPLab --ref <정확한-40자리-commit> --output Temp/<작업>/ci.json
```

verify_validation.py는 evidence의 checks.json이 지정한 결과 건수·문서·asset, test-inputs.json의 LF 정규화 source hash, preserved-inputs.json의 raw bytes, final Console/Editor 증거를 확인한다. Unity 테스트를 실행하지 않는다. 최소 실제 실행은 [Phase 1 검증](../doc/validation/scene-transition-contracts/README.md)에 기록한다.

check_github_ci.py는 기존 Git credential helper를 사용해 정확한 commit·main 보호·workflow/check/status/run을 읽는다. 비밀값을 출력하거나 저장하지 않는다. 현재 자동 확인은 **보호되지 않은 main + CI 미구성**인 경우만 완료하며 이것을 CI 성공으로 표시하지 않는다. CI/보호 설정이 있으면 report를 보존하고 실패 종료하므로 필수 검사와 정책을 별도로 검토해야 한다. 전체 정책 해석·페이지별 결과 통합은 지원하지 않으며 configured CI에 대한 자동 병합 승인을 제공하지 않는다.

run_unity_tests.py는 절대 project 경로의 기존 ready Editor만 사용하고 결과를 지정한 JSON에 보존한다. Editor를 시작하거나 교체하지 않는다. 실행 전 Console의 C# compiler 오류가 있으면 중단해 컴파일 실패 뒤 남은 Connector 결과를 재사용하지 않는다. CLI domain reload 연결 단절 시 이번 CLI 프로세스 PID와 실행 시간에 해당하는 Connector 결과만 읽는다. 다른 실행/프로젝트 결과나 0건은 성공으로 대체하지 않는다. Console·빌드·시각 UX 검증은 별개다.

```powershell
python tools/run_unity_tests.py --self-check
python tools/run_unity_tests.py --project C:\Users\PC\Projects\MyLab --mode PlayMode --filter MyLab.Core.Tests.GameSceneEntryTests --output Temp/MyCheck/play.json
```

최소 실행 검증은 [Phase 2 증거](../doc/validation/game-scene-entry/README.md)의 전체 Edit/Play와 결과 파서 self-check다.

run_unity_tests.py는 연속 실행 시 같은 Editor가 ready로 돌아올 때까지 최대 30초 대기한다. 다른 Editor를 시작하거나 검증을 대체하지 않는다.

정확한 실행 전 `no Unity instances running` 접수 오류에만 동일 project/Editor PID를 재확인하고 한 번 재접수한다. 실제 테스트 실패·모호한 결과는 재실행하지 않는다. 결과에는 Editor PID와 각 접수 시도를 남긴다. [구역 검증 도구 기록](../doc/validation/scene-areas/driver-checks.json)은 최초 접수 실패와 오류 분류 self-check Red/Green을 보존한다.

capture_validation_inputs.py는 명시한 기준 commit과 보존 raw hash를 입력받아 현재 코어·의존성·검증 도구 hash를 evidence에 기록한다. 보호 파일이 달라지면 중단한다. 텍스트/Binary 정규화 self-check와 실제 실행은 [로더 증거](../doc/validation/scene-loaders/README.md)를 따른다.

```powershell
python tools/capture_validation_inputs.py --self-check
python tools/capture_validation_inputs.py --evidence doc/validation/scene-loaders --base acb636c933f2eea29f46705d233f4e0dba436965 --preserved Temp/GameScenesTrack/preserved-hashes.json
```

P6에는 승인된 별도 소비 프로젝트 빌드/Player와 이미 빌드된 sample Player 실행 도구를 제공한다. run_core_consumer.py만 명시적으로 별도 batch Editor를 시작하며 원래 MyLab Editor의 검증을 대체하지 않는다. run_scene_player.py는 Editor를 시작하지 않는다. 두 도구는 숨긴 프로세스의 PID·fresh log/result·version·exit·nonzero 관찰 수를 검증하고 evidence를 보존한다. 기존 output을 재사용하지 않으며 파일 삭제·Git 통합은 하지 않는다. 기본 소비 manifest는 UniTask·Addressables와 필요한 Unity built-in만 포함하며 Input 모듈은 제외한다. `--include-input`은 Runtime allowlist와 Input System 1.19.0을 더해 복사본에서 컴파일·Windows Mono Player의 가상 키보드 rebind, lease 복원, JSON override/reset, graceful shutdown을 확인한다. 이 복사본에는 소스 ProjectSettings 전체 대신 `PlayerSettings.activeInputHandler: 1`만 포함한 최소 설정을 생성한다. 증거 경로는 scene-integration 또는 input-system 아래를 지정한다.

```powershell
python -B tools/run_core_consumer.py --self-check
python -B tools/run_core_consumer.py --project C:\Users\PC\Projects\MyLab --unity <기존-Editor.exe> --output Temp/<고유-소비자폴더> --evidence doc/validation/input-system/<실행폴더> --include-input
python -B tools/run_scene_player.py --self-check
python -O -B tools/run_scene_player.py --self-check
python -B tools/verify_validation.py --evidence doc/validation/scene-integration
```

실행 인수는 각 도구의 `--help`를 사용한다. 실제 두 모드 Player와 소비 실행·driver 실패·검증 제한은 [P6 증거](../doc/validation/scene-integration/README.md)에 기록한다.
