# 2026-10-12 · UI 전체 수락과 main 통합

- 기준: P7 branch와 후보 `5af64b96b8d7f1793d26465550077ac1a5f8cd25`, main `8ce768d96f79ed8328143bebc31e77f060f51205`. 사용자는 잔상 수정뿐 아니라 전체 UI 최종 항목도 확인 완료했다고 명시했다.
- 새 검증: 후보 그대로 Single/Additive 소비 프로젝트를 순차 실행했다. 합계 batch build2/artifact4/graphics Player8, 모두exit0·timeout0. Sample80/78checks, 각각양수draw30/min12·inventory7·Continue4·cleanup2·ManagerStopped/registered/owned0. Synthetic 실행의 physical/visualfalse와 직접 사용자 수락을 구분한다. [실제 근거](../validation/ui-system/integration-20261012/README.md).
- 계승: captured476의 CRLF→LF 정책으로473일치, 변경3개는 sample Builder와 두 Bootstrap scene이다. Runtime·테스트·의존성은 같아 기존624/최근UI86을 계승한다. 새 원본unit suite0, base/noInput0. 기존1장치 성능 해석을 새 반복값으로 덮어쓰지 않았다.
- 담당: 기존수명 agent는 변하지 않은Runtime/테스트190입력과 통합 gate를 읽기 검토했고, 기존문서 agent(gpt6-luna/low)는 지정7문서만 갱신했다. 부모가 검증 실행·증거·Editor 복원·Git을 맡았다.
- 보존: InitScene·사용자6raw파일을 원래대로 복원했고 originalEditor는 InitScene/idle/Console0이다. 새 코드·패키지·upm·0.0.1tag/Release 변경은 없다. 사용자 기기 상세는 별도 제공되지 않아 특정gamepad/다른플랫폼 보장을 추가하지 않는다.
- 통합: 최종 문서/증거를 UItrack에 보존한 뒤 ancestry/CAS fast-forward로 main을 진행한다. 같은 트리로 main을 전환하여 Editor에 source 제거/재생성 import를 유발하지 않는다. 정상main push·SHA·원본 보존 확인 뒤 승인된 UIbranch10개(원격track1개)를 정리한다. [사전tip](../validation/ui-system/integration-20261012/branch-tips-before.json). 원본 commit을 보존하며 강제branch삭제/이력 재작성은 하지 않는다.
- 임시 공간: 소비 검증2개와 기존수락대기 소비2개, 복원 완료manualreview1개만 처리할 계획이다. 최종 원문 검증 근거는doc에 보존한다. Unity는 열어두며 새 PC 종료/절전 지시는 없다.

- 실제 Git 마감: main을 `7b606586038883d4a38841e76e4c969d116306b1`로 fast-forward·푸시한 뒤 서버 SHA 일치를 확인했다. Phase8개는 `branch -d`로 삭제했고, 검증 후 소비4개/manual review1개를 native PowerShell의 workspace 경계·프로세스 사용 확인 후 제거했다. 마지막 문서/main push 뒤 UItrack/P7 두 로컬과 원격track을 SHA 조건으로 정리한다. 이후 직접 복원은 main에서 도달 가능한 원본 commit으로 가능하다.
