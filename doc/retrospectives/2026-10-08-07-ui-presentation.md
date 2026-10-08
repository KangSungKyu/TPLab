# UIContext P3 HUD·Popup·Canvas

2026-10-08. 기준 codex/game-ui-p3-presentation /15e5eb65edea47ff4e830be54a4e90ea081df14e. 최신 검증 track에서 생성한 branch를 사용했다.

source `18666acae25b04c1c63ca49d8c00ca2ee67da308`: borrowed host, HUD 후보 준비 후 교체, 논리 owner tree의 child-first 종료와 실제 Canvas/sibling 순서, 전용 Canvas 숨김·reuse를 구현했다. UIHandle.ViewObject는 원 clone이고 renderer-only native root는 owned RectTransform wrapper다. Unity에서 child Canvas를 끄면 Graphic이 상위 Canvas로 fallback하며 CanvasGroup도 같은 GO에 두 개 추가할 수 없었다. wrapper mask로 이를 차단하고 프로젝트 CanvasGroup/host 설정을 보존했다. Renderer-only reuse는 GO가 활성인 보관이며 표시 수명 정리는 hook/cleanup이 맡는다.

[실제 검증](../validation/ui-system/p3/README.md): Red Edit4실패4/Play11실패11, 최종 UI Edit18/18+Play30/30 실패0/skip0, compile/제품 Console0, source375와 보호6 unchanged. First Green Play3건은 실제 nested Canvas flag·native 렌더 frame 관측 fixture를 보정했고 Runtime을 바꾸지 않았다. 결과 없는 전송 시도와 뒤늦게 도착한 Edit16/18(Timeout2)을 별도 보존했다. 설치 UniTask의 Editor update는 Time.frameCount 갱신을 강제하지 않으므로 EditMode 테스트3개 파일의 NextFrame을 Yield로 바꿨다. 최종3초대 Edit18 완료로 해당 의존을 제거했다. Play native 프레임 대기는 유지한다.

재사용 테스트 결과file 도구는 source에 포함하고 self-check와 원본 Editor 실제 실행을 확인했다. phase source와 XML/사람·AI 문서/증거·회고를 기록하고 원 commit을 유지해 track에 fast-forward 통합한다. 사람/AI 임시 문서2개는 이관 뒤 소유 경로만 정리했다. P4/P5/P6/P7 Temp 초안은 재개에 필요해 각 실제 Red 반영 때까지 보존한다.

소비/Player/Profiler/최종 UX는 P7 미실행이다. main8ce768d·기존 사용자6개·upm/version/tag/Release는 보존한다. 다음 P4는 optional borrowed EventSystem 주입, native UI gate와 선택 InputSystem adapter, 독립 lease·raw 해제+frame 경계의 actual Red부터 진행한다. main 병합/branch 삭제는 최종 사용자 수락 후다.
