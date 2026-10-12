# 2026-10-12 · UI 예제 프레임 잔상

- 요청: 버튼 후 글자가 겹치는 현상. 사용자가 Play 화면을 그대로 두고 조사하도록 승인했다. `codex/game-ui-p7-validation`, 기준 `bb4372bb17ef9e471e28ff27f835a1ccb4e1581e`를 재사용했다.
- 재현: Runtime Status는 최신문자열 하나였으나 실제 Game 캡처에는 과거문구·닫힌Popup·cover가 남아 있었다. 열린sample의 Camera0, Overlay Canvas만 존재했다. runtime에 임시SolidColor Camera를 추가한 같은Play/같은상태에서 이전픽셀이 사라졌다.
- 변경: sample Builder와 두Bootstrap scene에 common-root child 배경Camera 하나만 추가. geometry culling0, opaque SolidColor, fullviewport/depth-100. Single/DDOL과 retained Additive 소유권을 그대로 따른다. UIContext/Core/Input Runtime·public API·prefab/meta 변경 없음. 사람/AI 예제설명·수락가이드 함께 갱신.
- 검토: 기존수명하위담당은 모델/추론상속을 유지하고 sample geometry/sorting/설치URP camera fallback을 읽기검토했다. 부모가 실제화면·수정·Unity/Git을 맡았다. 별도Canvas/URP의존성을 추가하지 않았다.
- 검증: 원본Unity6000.3.18f1/PID24376에서 두entry 각각 최초Continue→A/B/C→B/C해제→Hub→Main automatic전환/Ready·camera유지. 실제Game screenshots에서 잔상 소실. compiler idle와 등록13/6/7 유지, 검증Console0. [원문](../validation/ui-system/sample-clear-20261012/README.md).
- 한계: 새unit tests0·Player builds0·performance0. UI Runtime이 같은 기존86/624와 새Editor command-path 확인을 구분한다. Console을 검증전에 비웠으며 사용자상태에서 관찰한 잘못된명령순서/이미선택된HUD 예외2는 이잔상의원인이 아니다. 실제입력/여러화면비/최종수락 미완료. 기존P7 Players는 camera수정전이라 최신검증본으로 재표시하지 않는다.
- 보존: 사용자5파일 raw bytes와수정3asset meta unchanged. UI검증을위해 build entry를Single로잠시선택후5Additive씬으로돌렸으며 temporaryraw와최초진단baseline의해시차이를summary에명시했다. 원본EditorBuildSettings backup과InitScene 복원정보는 active manualreview folder에유지하고 buildsettings는stage제외. runInBackground는해당Play에서만true로하고false복원.
- 종료: Additive 예제를Play로열어두고manual Continue 정책을기본on으로복원했다. 예제수정/증거를UItrack에보존하며 main/branch삭제·Unity/PC종료는수행하지않는다.
