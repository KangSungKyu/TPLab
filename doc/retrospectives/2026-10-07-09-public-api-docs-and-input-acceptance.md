# 공개 API 문서와 입력 최종 수락

2026-10-07. 상태: 문서 작성·리뷰·정적 검증 완료. 최종 Git 통합 기록은 [통합 증거](../validation/input-system/p4/final-integration/README.md)를 따른다.

- 요청: 입력 확인 완료를 수락하고 사람/AI README·API 문서를 실제 작성한다. loading 상태만 답하며 progress/팁/proceed 확장은 구현하지 않는다.
- 기준: `codex/input-system-track` HEAD `502ab4c4b53e1dc8740fc105f6d2e1e1532365dd`; main `ce290878da51c653eaa219aecc45e6bf61ac755a`. 문서 새 단위를 위해 `codex/public-api-documentation` 생성. 기존 dirty4개와 보호7개 보존.
- 변경: 사람 README/API index+7모듈, AI README/API index+7모듈; source/XML 계약·의존성·준비/해제·오류/취소·예제·검증 제한; 수락/색인/기존 명세의 오래된 현재 상태 수정. runtime·package·settings 변경 없음.
- 결정: source9305를 문서 기준으로 고정하고 테스트 증거의 실제 source 입력을 대조한다. 선언/설명 예제는 발췌 NotRun, 기존 실행 smoke는 별도 증거 연결. 프로젝트 외부 라이선스 미정과 다른 Unity/플랫폼 미검증을 유지한다. 외부 제공본 배포 완료는 주장하지 않는다.

| 담당 / 모델 | 목적·허용 경로 | 상태 |
|---|---|---|
| 부모 | 공유 README/index, DataTables/Input/Editor 양쪽 문서, 리뷰·보호/검증·회고·Git | 문서 작성·최종 통합 담당 |
| /root/input_layers 재사용 / gpt-6.1-sol high | 수명·취소 계약 검토에 기존 문맥 재사용. doc/api 및 doc/ai/api의 Lifecycle.md·SceneManagement.md만; 이후 부모 문서 읽기 전용 리뷰 | 작성·리뷰 완료; 비버튼 release 기본값 설명1건 보정 |
| /root/input_validation_plan 재사용 / gpt-6-luna low | 확정 API의 제한된 문서화. doc/api 및 doc/ai/api의 Pooling.md·Resources.md만 | 한국어 문서·정적 대조 완료·동결 |

- 문제/해결: 기존 씬 설계 첫 문단의 후속 단계 표현이 실제 구현보다 오래되어 현재 상태를 갱신했다. 문서 링크 검사에서 실제 Samples/Runtime 및 tools/core-consumer/templates 위치를 확인해 잘못된 링크를 수정했다. 새로운 generator/문서 배포 도구는 추가하지 않았다.
- 검증: [정적 검사](../validation/documentation/public-api-checks.json), 기존 p4 saved result·source304/보호7·GUID·Console 증거 대조. 새 Red/Green/Unity/Player/제품 Console 실행0건; 동작 변경이 없어 기존 실행 입력 일치로 gate를 판단한다. 기존 실제 Edit258/258·Play217/217 실패0·skip0과 사용자의 입력 수락을 구분한다.
- Git: 문서 commit/push, input track→main 통합과 해당 phase/guide/docs/track 정리를 기존 승인 범위에서 진행한다. 정확한 main/원격 SHA·각 tip·삭제 후 관찰은 통합 증거에 남긴다. unrelated branch 일괄 정리/사용자 dirty stage 없음.
- 다음: loading progress/팁/proceed UI는 초안·미구현. 실제 외부 제공 시 배포 라이선스 결정, 제공 source/tag 기준 설치/예제/링크와 해당 소비 프로젝트 확인이 필요하다. 문서 갱신은 계약 변경과 같은 단위에서 한다.
