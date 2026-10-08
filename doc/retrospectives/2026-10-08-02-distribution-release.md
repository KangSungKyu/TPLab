# 0.0.1 첫 정식 Release

2026-10-08. 상태: 공개 및 발행 검증 완료. 브랜치·장치 마감의 관측 결과는 원본 tplab/releases/0.0.1/operations.json에 기록한다. 기준 track c4ab282, 사용자 수락 반영/main 통합4a7b0f8, 검증 source `de879b9396ddae0523bd3ab86939b679b383dd92`. 태그는 검증 source에 고정한다.

사용자가 최종 확인 문서의 항목을 실행·확인했다고 명시해 남은 human gate를 완료했다. 원격 main/트랙/dirty를 확인하고 main을 fast-forward 병합·push했다. 사용자 파일6개 hash를 보존했으며 source가 main에서 도달 가능하고 tag peel과 같음을 확인했다. 정상 Refresh 뒤 기존 Editor의 컴파일 준비/C# 오류0을 확인했다. 기대된 이전 test 예외와 제품 Console 무오류 주장은 구분했다.

실제 tag URL 설치는 세 package/전체 Samples를 대상으로6process, build3개 오류0, sample24checks를 통과했다. [Release](https://github.com/KangSungKyu/TPLab/releases/tag/v0.0.1) 첨부물8개를 공개 후 인증 없이 다운로드해 원본 bytes와 비교했다. draft와 공개 상태를 구분하고 기존 Release/asset을 교체하지 않았다. API 인증은 Git credential helper에서 메모리로만 읽고 cross-host redirect에는 전달하지 않았다. 생성 시점 manifest는 바꾸지 않았으며 후속 gate/공개 다운로드 기록을 별도로 보존했다.

사람/AI README·문서 색인·배포 현황을 갱신하고 설치/API/라이선스/검증 안내를 제공했다. package 입력은 수정하지 않는다. source의 Edit286/Play253과 Git/tarball8구성 증거는 P3, 신규 tag/공개 다운로드는 [P4](../validation/distribution-release/README.md)가 소유한다. 타PC/다른Unity·플랫폼/IL2CPP/remoteAddressables·전체 물리장치는 미검증이다.

마감: 최종 문서 tip을 main에 보존하고 dev-build track/P1/P2/P3의 tip·원격·main ancestry·worktree 미사용을 확인해 이번4개 브랜치를 정리한다. 필요한 Release 파일·raw log·검증 프로젝트 소스와 최종 운영 기록은 원본 tplab/releases/0.0.1에 보존하고 소유한 native worktree/임시 출력만 제거한다. 정상 Unity 저장·종료를 확인한 뒤 사용자가 요청한 PC 종료를 실행한다. 저장으로 발생한 사용자 자산 변경을 commit하지 않는다. 종료 관측과 정확한 삭제 tip/main SHA는 마감 운영 기록 및 최종 보고에서 확인한다.

로컬 보존 ZIP은 샘플의 Unix epoch 파일 날짜로 첫 생성이 실패했다. 표준 strict_timestamps=False로 ZIP 범위에 날짜만 맞춰 재생성하고 CRC를 확인했다. 원본 파일 bytes나 공개 첨부물은 변경하지 않았다. [보존 기록](../validation/distribution-release/cleanup-preparation.json)을 따른다.
