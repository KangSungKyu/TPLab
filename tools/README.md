# 재사용 검증 도구

명령의 `C:\Users\PC\Projects\MyLab`은 현재 PC의 실제 checkout 경로 예시다. `--project`에는 각 PC의 절대 경로를 사용하며 코드·assembly·메뉴는 `TPLab`이다. [이름 변경과 검증](../doc/TPLAB_NAMING.md)을 참고한다.

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
python tools/run_unity_tests.py --project C:\Users\PC\Projects\MyLab --mode PlayMode --filter TPLab.Core.Tests.GameSceneEntryTests --output Temp/MyCheck/play.json
```

최소 실행 검증은 [Phase 2 증거](../doc/validation/game-scene-entry/README.md)의 전체 Edit/Play와 결과 파서 self-check다.

run_unity_tests.py는 연속 실행 시 같은 Editor가 ready로 돌아올 때까지 최대 30초 대기한다. 다른 Editor를 시작하거나 검증을 대체하지 않는다.

정확한 실행 전 `no Unity instances running` 접수 오류에만 동일 project/Editor PID를 재확인하고 한 번 재접수한다. 실제 테스트 실패·모호한 결과는 재실행하지 않는다. 결과에는 Editor PID와 각 접수 시도를 남긴다. [구역 검증 도구 기록](../doc/validation/scene-areas/driver-checks.json)은 최초 접수 실패와 오류 분류 self-check Red/Green을 보존한다.

capture_validation_inputs.py는 명시한 기준 commit과 보존 raw hash를 입력받아 현재 코어·의존성·검증 도구 hash를 evidence에 기록한다. 보호 파일이 달라지면 중단한다. 텍스트/Binary 정규화 self-check와 실제 실행은 [로더 증거](../doc/validation/scene-loaders/README.md)를 따른다.

```powershell
python tools/capture_validation_inputs.py --self-check
python tools/capture_validation_inputs.py --evidence doc/validation/scene-loaders --base acb636c933f2eea29f46705d233f4e0dba436965 --preserved Temp/GameScenesTrack/preserved-hashes.json
```

P6 도구는 기존 TPLab Editor의 검증과 Windows Mono sample Player 실행을 지원한다. `run_core_consumer.py`만 명시적으로 별도 batch Editor를 시작하며 원래 TPLab Editor의 검증을 대체하지 않는다. `run_scene_player.py`는 이미 빌드된 Player만 실행하고 Editor를 시작하지 않는다. 두 도구는 숨긴 프로세스의 PID·fresh log/result·version·exit·nonzero 관찰 수를 검증하고 evidence를 보존한다. 기존 output을 재사용하지 않으며 파일 삭제·Git 통합은 하지 않는다. 기본 소비 manifest는 UniTask·Addressables와 필요한 Unity built-in만 포함하며 Input 모듈은 제외한다. `--include-input`은 Runtime allowlist와 Input System 1.19.0을 더해 복사본에서 컴파일·Windows Mono Player의 virtual keyboard rebind, lease 복원, JSON override/reset, graceful shutdown을 확인한다. 복사본에는 소스 ProjectSettings 전체 대신 `PlayerSettings.activeInputHandler: 1`만 생성한다. 두 도구의 evidence root에는 `doc/validation/scene-loading`도 허용된다.

Sample Player는 현재 TPLab Editor에서 `TPLab.Samples.SceneTransitions.Editor.SceneTransitionSampleBuilder.BuildWindowsMono(bool single, string outputDirectory, string evidencePath)`로 만든다. output은 새 경로 `Temp/GameScenesTrack/ScenePlayers/<run>` 아래, build evidence는 새 JSON `doc/validation/scene-loading/<name>.json` 아래에 지정한다. Builder가 scripting backend, Editor scene setup, Build Settings와 그 원본 bytes를 복원한다. Build 후 `run_scene_player.py` 기본 smoke는 10 checks다. `--loading-presentation`은 sample 자동 진행 UI를 켜고 정확히 12 checks와 progress/reveal/proceed/release/two-cover observations를 요구한다. 이 batch 경로는 Manual Proceed나 실제 physical input/visual acceptance를 검증하지 않는다.

```powershell
python -B tools/run_core_consumer.py --self-check
python -B tools/run_core_consumer.py --project C:\Users\PC\Projects\MyLab --unity <기존-Editor.exe> --output Temp/<고유-소비자폴더> --evidence doc/validation/input-system/<실행폴더> --include-input
python -B tools/run_core_consumer.py --project C:\Users\PC\Projects\MyLab --unity <기존-Editor.exe> --output Temp/SceneLoadingConsumer-01 --evidence doc/validation/scene-loading/consumer-01
python -B tools/run_scene_player.py --self-check
python -O -B tools/run_scene_player.py --self-check
python -B tools/verify_validation.py --evidence doc/validation/scene-integration
```

Windows Mono sample build와 automatic loading smoke 예시 (모두 고유한 새 output/evidence 이름으로 실행):

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab exec 'TPLab.Samples.SceneTransitions.Editor.SceneTransitionSampleBuilder.BuildWindowsMono(false, "Temp/GameScenesTrack/ScenePlayers/Loading-Additive-01", "doc/validation/scene-loading/loading-build-additive-01.json"); return null;'
python -B tools/run_scene_player.py --project C:\Users\PC\Projects\MyLab --player C:\Users\PC\Projects\MyLab\Temp\GameScenesTrack\ScenePlayers\Loading-Additive-01\SceneTransitions.exe --output C:\Users\PC\Projects\MyLab\Temp\GameScenesTrack\PlayerRuns\Loading-Additive-01 --evidence C:\Users\PC\Projects\MyLab\doc\validation\scene-loading\player-run-additive-01 --mode additive --expected-checks 12 --loading-presentation

unity-cli --project C:\Users\PC\Projects\MyLab exec 'TPLab.Samples.SceneTransitions.Editor.SceneTransitionSampleBuilder.BuildWindowsMono(true, "Temp/GameScenesTrack/ScenePlayers/Loading-Single-01", "doc/validation/scene-loading/loading-build-single-01.json"); return null;'
python -B tools/run_scene_player.py --project C:\Users\PC\Projects\MyLab --player C:\Users\PC\Projects\MyLab\Temp\GameScenesTrack\ScenePlayers\Loading-Single-01\SceneTransitions.exe --output C:\Users\PC\Projects\MyLab\Temp\GameScenesTrack\PlayerRuns\Loading-Single-01 --evidence C:\Users\PC\Projects\MyLab\doc\validation\scene-loading\player-run-single-01 --mode single --expected-checks 12 --loading-presentation
```

Each run output directory and build JSON filename must be unused. Use distinct fresh names for reruns. `--loading-presentation` opts into **automatic** mode only; run the [manual UI acceptance](../doc/SCENE_LOADING_ACCEPTANCE.md) separately. Build/sample results and current limitations are recorded in the relevant validation evidence and [scene-loading track](../doc/SCENE_LOADING_TRACK.md).

실행 인수는 각 도구의 `--help`를 사용한다. 실제 두 모드 Player와 소비 실행·driver 실패·검증 제한은 [P6 증거](../doc/validation/scene-integration/README.md)에 기록한다.
