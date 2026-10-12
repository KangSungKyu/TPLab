# 2026-10-12 · UI compiler 등록 누락 복구

- 요청: 현재 Editor 컴파일 오류 확인. 기존 `codex/game-ui-p7-validation`, HEAD `e4a3df6b2bf472f36c09991bf9073b4f727a1bcc` 재사용. 사용자 변경6개와 main 보존.
- 원인 경계: UIPresentation을 찾지 못하는 CS0246 5건. 세 source/MonoImporter/GUID/asmdef는 정상이지만 실제 compiler/native-fed 등록 목록에서 빠져 있었다. Runtime12/13·Edit5/6·Play6/7; 최초 발생 계기는 확정하지 못했다. 지난 P7의 managed registry 임시 union은 native 복구가 아니었다.
- 복구: 해당 세 asset만 표준 MoveAsset/import/restore로 재등록했다. 코드·meta raw bytes와 GUID는 유지했다. 기존 소스를 합치거나 새 helper/프로젝트 자동 hook을 추가하지 않았다. Library/사용자 설정·scene을 삭제/저장/재직렬화하지 않았다.
- 검증: 원본 Editor24376에서 첫 compile/domain reload → 일반 Refresh+CleanBuildCache 재컴파일 → UI Edit33/33·Play53/53 → 최종registry7347/입력13·6·7/Console0. 누락됐던 presentation tests18개 모두 포함. [증거](../validation/ui-system/compile-registry-20261012/README.md).
- 보존: P7 source476개, 해당3 source/meta와 사용자6개 raw bytes unchanged. Runtime/public API 변경 없음으로 XML/사람·AI API 계약 수정 대상은 없다. 기존 source SHA의 과거 검증을 새 테스트로 재표시하지 않는다.
- 한계: Editor 재시작·새 full624 회귀/Player/performance·실제입력/시각 검증은 이번 단위 미실행. 현재 세 차례 등록 유지와 컴파일/실제UI86을 확인했다. Source는 본래 정상이며 현재 Native discovery 상태를 복구한 것으로, 다른 Unity 환경의 모든 재발을 보장하지 않는다.
- 기록·통합: 현재 작업 branch의 증거·문서·회고만 기록해 UI track에 통합한다. UI 최종 사용자 수락이 남아 main/branch 정리는 보류한다. 새 PC 절전 요청은 이 오류 확인에 포함되지 않았다.
