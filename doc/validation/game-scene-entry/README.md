# GameSceneManager Phase 2 검증

2026-10-06. 기준 main/ab8d3ff, 작업 codex/game-scene-entry. MyLab Unity 6000.3.18f1, Connector 0.4.1, 기존 Editor PID 23176. [현재 계약](../../BOOTSTRAP_SYSTEM.md)은 최초 진입만 구현한다.

| 실제 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 영속 공용 root 설정 Red | 1 / 0 / 1 / 0 | [red-edit.json](red-edit.json) |
| 실제 씬 진입 뒤 manager 공개 Red | 1 / 0 / 1 / 0 | [red-play.json](red-play.json) |
| 비동기 cover 뒤 common hook 재진입 Red | 1 / 0 / 1 / 0 | [red-async-cover.json](red-async-cover.json) |
| 소유 씬 밖으로 이동한 게임 root Red | 1 / 0 / 1 / 0 | [red-moved-root.json](red-moved-root.json) |
| Bootstrap 설정 Green | 28 / 28 / 0 / 0 | [green-edit.json](green-edit.json) |
| Bootstrap 수명·callback Green | 21 / 21 / 0 / 0 | [green-bootstrap.json](green-bootstrap.json) |
| 영속/Single·직접 코드 API Green | 7 / 7 / 0 / 0 | [green-entry.json](green-entry.json) |
| 최종 전체 EditMode | 182 / 182 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 최종 전체 PlayMode | 115 / 115 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

Red는 manager 위임 전 실제 실행 실패다. PlayMode는 성공적으로 native Additive 진입한 뒤 기존 Bootstrap에 Manager가 없음을 검사했다. 초기 CLI 연결 단절의 해당 fresh Connector 결과를 보존했다. 최종 리뷰의 비동기 cover 이후 common-installer 재진입 Red도 실제 실패를 확인하고 flow/root 호출 경계에서 보완했다. Green에서 reflection을 직접 타입 API 검증으로 정리했다. focused 설정/entry 결과 뒤 validation helper를 확장했고 최종 전체 회귀는 최종 C# 입력을 포함한다.

게임 root가 소유 씬 밖으로 이동한 Red를 확인해 준비 검사와 소유 서비스 정리를 보완했다. 공용 root는 최초 소유 씬을 유지해야 하며 후보 안의 빌린 공용 root를 언로드하지 않는다. 최종 PlayMode는 실제 native Single로 이전 씬이 없어지는지, 영속 SceneOwned/Singleton 공용 root 유지, Additive Bootstrap 유지, 준비·화면 대기, await/owner 취소 분리, 늦은 native 완료, 마지막 Single 씬 잔여 상태, 명시적 종료로 정리 완료, 동기 hook/installer 순환 거부, 종료/callback 오류 집계, 외부 씬 집합/active 변경, 외부 씬 보존을 검증했다. 가림막과 입력의 시각 UX를 확인한 증거는 아니다.

[Native Editor 결과](native-editor.json)는 기존 BootstrapEditorCheck를 현재 evidence 경로와 Single/비영속 root 시나리오로 실행했다. 실제 BuildPipeline과 Play gate가 모순을 차단하고 검증 씬을 제거했다. BuildResult=Unknown은 PrepareForBuild의 BuildFailedException에 의한 중단이며 성공 Player 빌드가 아니다. [예상 native 로그](native-expected-console.json)가 해당 사유를 보존한다. build list나 사용자 씬을 저장·변경하지 않았다.

[83개 최종 입력 hash](test-inputs.json)는 C#/assembly·fixture·의존성·검증 도구를 대조한다. text는 CRLF→LF, DLL은 raw bytes다. Unity 입력은 최종 전체 실행에 대응하고 Python 도구의 결과 파서 self-check도 실제 통과했다. 현재 Git index와 hash를 대조해 커밋 대상이 검증 입력과 같은지 확인한다. [보존 hash](preserved-inputs.json)의 사용자 변경 4개와 EditorBuildSettings raw bytes는 시작 상태와 같다. GUID와 문서 링크·fixture 부재·공백도 검사한다.

[반복 보완](iteration-notes.json)은 최초 Bootstrap 11/5/6/0, Single 5/2/3/0 실패 원인과 해결을 구분한다. Scene handle의 기본 정렬 오류는 Scene 집합 비교로, native Single의 이미 active인 씬 설정과 OnDestroy 종료 오류 재관찰은 실행/종료 경계 수정으로 해결했다. [첫 Bootstrap 결과](iteration-bootstrap.json)와 [첫 Single 결과](iteration-entry.json)는 최종 성공 gate로 사용하지 않는다.

컴파일 완료와 [Editor ready](final-status.txt)를 확인했다. 최종 테스트의 예상 오류 로그 10개는 [원본](expected-test-console.json)으로 보존했다. 기존 Singleton/provider/root의 LogAssert 시나리오이며 통과를 Console 무로그로 표현하지 않는다. 보존·clear 뒤 [새 Console 오류/경고 0](final-console.json)을 확인했다. CI 설정과 정확한 원격 커밋 확인은 [CI 기록](ci-policy.json)을 따른다. CI 미구성을 CI 성공으로 표시하지 않는다.

재실행:

```powershell
python tools/run_unity_tests.py --project C:\Users\PC\Projects\MyLab --mode EditMode --output Temp/MyCheck/edit.json
python tools/run_unity_tests.py --project C:\Users\PC\Projects\MyLab --mode PlayMode --output Temp/MyCheck/play.json
python tools/run_unity_tests.py --self-check
python tools/verify_validation.py --evidence doc/validation/game-scene-entry
```

Native gate는 existing evidence directory를 지정해 `BootstrapEditorCheck.Run("doc/validation/game-scene-entry/native-editor.json", true)`로 실행한다. 정적 verifier는 Unity 실행을 대신하지 않는다. CLI runner는 명시된 기존 ready Editor만 사용하고, domain reload 연결 단절 때도 이번 CLI PID/실행 시간에 해당하는 실제 결과만 복원한다. 이번 최종 전체 실행은 CLI가 정상 반환했다. 이동 root Red와 Bootstrap 21/21 Green에서는 runner가 해당 CLI PID의 Connector 파일 결과를 실제 복원했다. 마지막 Edit 뒤 discovery 1회 실패는 테스트 시작 전 발생했으며 동일 PID ready 확인 뒤 Play를 실행했다.

연속 교체·파생 tree·조건/정의, 소비 프로젝트 가져오기, 성공 Player 빌드/실행, 제품 cover/input UX와 Reload 비활성 반복 Play는 미검증이다. callback/installer가 await 뒤 자기 Bootstrap 공유 대기를 호출하는 순환은 자동 검출하지 못하며 계약으로 금지한다. UI 자산 변경은 없어 이번 단위의 사용자 직접 확인 gate는 없다. 임시 transcript/스크립트는 증거 보존 후 이번 작업의 명시 목록으로 제거한다.
