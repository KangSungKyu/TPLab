# UI P3 검증

기준 track `15e5eb65edea47ff4e830be54a4e90ea081df14e`, 최종 source `18666acae25b04c1c63ca49d8c00ca2ee67da308`. Unity6000.3.18f1/Connector0.4.1, 원본 TPLab Editor PID24376.

최종 [Edit18/18](final-edit.json) + [Play30/30](final-play.json), 실패0/skip0, compile/제품 Console 오류0이다. Edit 결과는 같은 설치 Connector가 완료한 [native 파일](final-edit.native.json)의 runId303ed314e92e4c3cb12d9f935257816f와 project/filter/mode를 대조했다. Play CLI29644의 실제 결과다. 입력375개와 사용자 보호6파일 unchanged를 확인했다. P1/P2 회귀30개와 P3 보강18개를 포함한다. 소비/Player/Profiler/최종 사용자 수락은 P7 미실행이다.

실제 Red는 [Edit4실패4](red-edit.json)/[Play11실패11](red-play.json), skip0이며 [당시 입력372개](red-inputs/test-inputs.json)를 보존했다. Play는 동일 invocation CLI4156의 connector 결과다. 추가3개 Play 회귀는 별도 Red를 주장하지 않는다.

첫 Green [Edit18/18](first-green-edit.json), [Play27/30](first-green-play.json)의 실패3건과 [당시 입력](first-green-inputs/test-inputs.json)을 보존했다. Runtime을 바꾸지 않고 fixture의 overrideSorting을 실제 active nested Canvas에서 설정·assert하고 최초 native 렌더 frame을 기다리도록 보정했다. 이후 [P3 presentation14/14](fixture-corrected-play.json)가 통과했다.

결과 없는 Edit 전송 [최초](transport-rejected-edit.json)/[재검증](transport-rejected-edit-final.json)은 테스트 통과/실패 수로 바꾸지 않았다. 열린 HTTP응답 의존을 줄이는 Edit 결과file 도구를 추가했다. 이 경로의 [첫 실제 결과](frame-dependent-edit.native.json)는 늦게 도착했고 Edit18 중16통과/2건180초 Timeout이었다. [입력375개](frame-dependent-inputs/test-inputs.json)를 보존했다. 설치 UniTask의 Editor update는 렌더 frame 갱신을 강제하지 않아, 기존 EditMode 테스트의 NextFrame 대기가 멈출 수 있었다. Edit 테스트3파일만 Yield로 보정하고 원본 Editor의 완료/idle을 확인한 뒤 최종18/18을 새 실행했다. 취소 조건부 호출 시 이미 실행이 끝나 실제 취소는 없었다. Runtime 수정이나 다른 Editor 결과 대체는 없었다.

[native Canvas fallback](native-canvas-observation.json), [CanvasGroup 단일 컴포넌트](native-group-observation.json), [overrideSorting 저장](native-override-observation.json)은 native 관찰이며 UIContext 테스트 결과와 구분한다. 정적 source/보호 bytes·GUID·문서 링크·최종 Editor 증거 검사를 같은 단위에서 수행한다.
