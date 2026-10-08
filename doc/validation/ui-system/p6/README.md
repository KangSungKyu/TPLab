# P6 설치/root 검증

원본 Unity 6000.3.18f1 Editor PID24376에서 실제 Red7(Edit2+Play5, 실패7/skip0) 후 Settings/Installer를 구현했다.

첫 Green 전체86은 Edit33 중1·Play53 중2 실패했다. [원본 실패](first-green-edit.json), [Play 실패](first-green-play.json), [진단](first-green-diagnosis.json)을 보존했다. 실제 enum probe에서 enumValueIndex999는 정상 enum1을 선택했고 intValue999는 공용 validator가 거부했다. 비활성 installer 검색은 includeInactive=true로 보정했고 Addressables 순서 fixture는 소유 empty catalog/prefs 복원 경계를 추가했다. Runtime 검증을 느슨하게 하거나 오류 로그를 숨기지 않았다.

수정 후 [Edit33/33](second-green-edit.json) + [Play53/53](second-green-play.json), 실패0/skip0. [입력416개](second-green-inputs/test-inputs.json), 보호6 raw bytes unchanged. Edit runId ba3018f212e14372905aed0a4c23962d, Play CLI30612/return0. 이 결과는 설치/API의 focused proof이며 예제·소비/Player·Profiler·시각/물리 입력 확인은 별도 gate다.

원본 Editor compiler registry가 기존 UI3 source를 빠뜨리는 현상을 유지했다. 매번 이전 source 전부를 보존하고 정확한 owned UI3을 일회성 compiler input에 보완했으며 영구 복구로 주장하지 않는다. 최종 독립 소비 프로젝트에서 새로 compile한다. 테스트 미시작 discovery 실패와 compile 실패는 실제 Red/Green 수로 세지 않는다.

P6 예제/최종 source commit·문서/회고·track 통합: 진행 중. P7 consumer helper 실제 Red4의 임시 증거는 [여기](p7-consumer-red.txt)에 보존하며 P7에서 이동한다. 사용자 최종 시각·실제 입력 수락, main 병합·branch 삭제: 대기.

## P6 최종 gate

Source `21f89e2580f08be3903724efa0a42e5ad0567c83`: [최종 Edit33/33](final-edit.json) + [Play53/53](final-play.json), 실패0/skip0. Play CLI8312 timeout 뒤 같은 connector run8312-1791458897117860600의 완료 결과를 복원했고 재실행하지 않았다.

[입력471개](test-inputs.json) 중 비보호470개는 Git 일치, 보호 SampleInput1은 제외했고 보호6개는 raw unchanged([source match](source-commit-match.json)). [신규19asset 생성](sample-authoring.json)과 씬 구성 보존·compileConsole0을 확인했다. Builder의 SceneTarget using 누락은 원래 실패를 보존하고 namespace만 보정했다. 자동 생성 YAML의 빈 값 공백을 보존하며 코드/문서에는 일반 diff --check, 생성 YAML에는 blank-at-eol만 제외한 검사를 적용했다. 소유 신규 파일15개의 CRLF→LF만 보정했으며 GUID/직렬화 값은 보존했다.

[최종 Editor](final-observation.json)는 ready, compile/update/Playfalse, InitScene dirtyfalse, 제품Console0이다. 기존 UI3 등록 누락은 계속되지만 실제 타입은 로드됐다. P7에서 독립 fresh consumer compile을 확인한다. 예제 Player/Profiler/물리 입력/시각UX와 사용자 수락·main 통합은 P7에서 기다린다.
