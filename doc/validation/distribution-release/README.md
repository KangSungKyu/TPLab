# 0.0.1 Release 검증

[정식 Release](https://github.com/KangSungKyu/TPLab/releases/tag/v0.0.1), source `de879b9396ddae0523bd3ab86939b679b383dd92`, tag `v0.0.1`. 사용자 최종 확인: Confirmed. [main/tag 통합](integration.json), [태그 소비 실행](core-consumer-20261008T014425Z-37332.json), [공개 다운로드](public-release.json)를 따른다.

P3에서 정확한 source의 Edit286/286·Play253/253, Git/tarball8/8, archive3개 재현과 사용자파일6개 보호를 확인했다. [P3 증거](../distribution-candidate/README.md)는 최초 실패도 보존한다. 이번 P4는 실제 #v0.0.1 공급원으로 세 package/전체 Samples를 resolve·compile·import·build·run했다. Editor3 + 기본Player1 + samplePlayer2 =6process, build3개 오류0, sample24checks. 각 installed payload와 lock/source SHA를 비교했다.

Release 첨부물8개를 draft 단계 및 공개 단계에서 각각 실제 다운로드했다. 공개 다운로드에는 인증을 사용하지 않았고 모든 파일이 로컬 원본과 exact bytes로 일치했다. 포함물: 세 .tgz, SHA256SUMS.txt, INSTALL.md, VALIDATION.md, distribution-manifest.json, RELEASE_VALIDATION.json. 각 패키지에 사람/AI API·MIT·제3자 고지가 있다. 생성 manifest의 publishable:false/NotRun은 생성 시점 값을 보존하며 [후속 gate](release-validation.json)와 공개 결과를 별도로 기록한다.

[병합 전 보호](before-integration.json)의 사용자파일6개는 P3와 동일했고 main 병합 후도 보존했다. [기존 Editor](original-editor-compile.json)는 최신 main import 후 ready/compile idle/C# 오류0이다. Console에는 의도된 이전 test 예외가 있으므로 제품 Console 전체 무오류로 표현하지 않는다. [exact CI](source-ci.json)는 미구성/unprotected 확인이며 CI 실행 성공이 아니다.

검증 범위: Unity6000.3.18f1·Windows Mono·동일 PC. 사용자는 최종 확인 문서의 실행을 확인했다. 모든 물리장치·타PC·Unity버전·플랫폼·IL2CPP·원격Addressables까지 확대하지 않는다. 브랜치/worktree 정리와 Unity/PC 종료는 release source/package 내용 변경과 구분하며 별도 마감 운영 기록을 남긴다.
