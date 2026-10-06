# BootstrapSystem 검증

2026-10-06, MyLab Unity 6000.3.18f1 / CLI Connector 0.4.1 / 기존 Editor PID 23176. 기준 main/ee8c0bf, 작업 codex/bootstrap-system. 계약은 [BOOTSTRAP_SYSTEM.md](../../BOOTSTRAP_SYSTEM.md)를 따른다.

| 실제 실행 | total / pass / fail / skip | 증거 |
|---|---|---|
| 설정 Red, fixture 보완 후 | 19 / 3 / 16 / 0 | [red-config.json](red-config.json) |
| 최초 진입 Red | 6 / 0 / 6 / 0 | [red-play.json](red-play.json) |
| 중복 Singleton 소유 Red | 22 / 21 / 1 / 0 | [red-singleton.json](red-singleton.json) |
| 종료 가림막 Red | 1 / 0 / 1 / 0 | [red-shutdown-cover.json](red-shutdown-cover.json) |
| 최종 전체 EditMode | 176 / 176 / 0 / 0 | [full-EditMode.json](full-EditMode.json) |
| 최종 전체 PlayMode | 96 / 96 / 0 / 0 | [full-PlayMode.json](full-PlayMode.json) |

최종 실행에는 Bootstrap 설정 22건·native 씬 진입 9건이 포함된다. 기존 테스트도 실제 다시 실행했다. [입력 hash](test-inputs.json)는 테스트 대상 C#/assembly·씬 fixture·의존성 파일을 소유한다. 실행 당시 raw hash를 보존하고, Git eol=lf checkout과 CRLF/mixed working copy 사이의 줄바꿈 차이를 허용하도록 텍스트는 LF 정규화 hash로 대조한다. DLL과 사용자 dirty 자산은 raw bytes를 검사한다. 정규화 내용은 검증된 e5ea2be Git blob과 일치한다. 전체 EditMode 실행 뒤 변경하지 않은 기존 SceneRootEditorTests의 AddComponent/Undo 정리에서 warning 6건이 남았고 [원본 로그](editmode-undo-warnings.json)로 보존했다. 제품 오류로 숨기거나 테스트 통과를 경고 없음으로 표시하지 않는다. 최종 C# 컴파일 성공과 [Editor ready](final-status.txt), 기대 로그를 보존한 뒤 [새 Console 오류/경고 0](final-console.json)을 확인했다.

[Native Editor 검사](native-editor.json)는 BootstrapEditorCheck.Run으로 실제 BuildPipeline.BuildPlayer가 잘못된 root를 BuildFailedException으로 차단하고, live 미저장 root를 감지하며, Play 진입을 취소하는지 검증했다. 전용 임시 씬은 제거했고 사용자 씬을 저장하지 않았다. PrepareForBuild에서 중단되어 BuildReport.summary.result는 Unknown이며 [실제 예외 로그](native-expected-console.json)가 원인을 증명한다. 성공적인 Player 빌드 결과로 표시하지 않는다.

[반복 보완 기록](iteration-notes.json)은 fixture 실패와 실제 결함을 구분한다. CLI Play 전환 연결 단절 1회는 해당 실행의 저장된 fresh Connector 결과 [6/6 원본](green-play-recovered.json)으로 확인했다. 최종 전체 실행은 CLI가 정상 반환했다. 임시 CLI transcript는 정규화 결과를 남긴 뒤 삭제했다.

[보존 hash](preserved-user-inputs.json)의 InitScene·render 자산 2개·SceneTemplateSettings는 작업 시작 bytes와 동일하다. Test Framework 사전 hook은 fixture를 build scene 목록에 임시 등록하고 종료 후 원래 목록·GUID와 serialized bytes를 복원한다. Build Settings의 추적 diff는 없다.

재실행:

```powershell
unity-cli --project C:\Users\PC\Projects\MyLab test --mode EditMode
unity-cli --project C:\Users\PC\Projects\MyLab test --mode PlayMode
unity-cli --project C:\Users\PC\Projects\MyLab exec 'MyLab.Core.Tests.BootstrapEditorCheck.Run(); return "dispatched";'
python doc/validation/bootstrap-system/verify.py
```

verify.py는 결과·source hash·사용자 입력 보존·문서 링크의 정적 대조이며 Unity 테스트 재실행을 대신하지 않는다. C#/문서 공백 검사에는 Unity 생성 YAML/meta의 빈 문자열 후행 공백을 제외하고 해당 GUID·참조를 별도 확인했다. [GitHub CI 설정](ci-policy.json)은 workflow/check/status 모두 0건이며 CI 미구성이다. 이를 CI 통과라고 표시하지 않는다. 프로젝트 Bootstrap 씬/게임 서비스를 자동 생성하거나 연결하지 않았다. 일반 GameSceneManager 씬 교체, 소비 프로젝트 가져오기, 성공 Player 실행, 시각 UX 및 Reload 비활성 옵션 반복 Play는 미검증이다. 사용자 직접 확인을 기다려야 하는 제품 연출 변경은 이번 scope에 없다.
